using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RoomFluidSimulator : MonoBehaviour
{
    public enum AutoPopulateMode { None, Grid, Random }

    [Header("References")]
    [Tooltip("If null, the script will try to find a RoomComponent in parents.")]
    public RoomComponent roomComponent;

    [Header("Rendering / Surface")]
    [Range(8, 128)] public int resolution = 24; // marching resolution (cells per axis)
    [Tooltip("Iso value for marching cubes. With cubic kernel try ~0.4-0.6.")]
    public float isoLevel = 0.5f;
    [Tooltip("Metaball visual radius (should match smoothingLength for best results).")]
    public float metaballRadius = 0.5f;

    [Header("Auto Populate")]
    public AutoPopulateMode autoPopulate = AutoPopulateMode.Grid;
    [Tooltip("Number of particles to generate (Grid will produce n^3 >= this).")]
    public int autoCount = 128;
    [Range(0f, 0.45f)] public float autoPadding = 0.05f;
    public int autoRandomSeed = 12345;
    public bool autoRegenerateOnBoundsChange = true;

    [Header("SPH / Particle Dynamics (Active Fluid)")]
    public bool simulateParticles = true;
    [Tooltip("Number of simulated particles.")]
    public int particleCount = 256;
    [Tooltip("Smoothing length (h) — controls interaction radius.")]
    public float smoothingLength = 0.6f;
    [Tooltip("Particle mass.")]
    public float particleMass = 1.0f;
    [Tooltip("Rest density (rho0).")]
    public float restDensity = 1.0f;
    [Tooltip("Pressure stiffness (k).")]
    public float stiffness = 200f;
    [Tooltip("Viscosity coefficient (mu).")]
    public float viscosity = 0.5f;
    [Tooltip("External acceleration (gravity).")]
    public Vector3 externalAccel = new Vector3(0f, -9.81f, 0f);
    [Tooltip("Self-propulsion force magnitude for active particles.")]
    public float propulsionStrength = 0.0f;
    [Tooltip("Noise magnitude applied to propulsion direction each step.")]
    public float propulsionNoise = 0.5f;
    [Tooltip("Simulation substeps per Unity frame for stability.")]
    [Range(1, 8)] public int simSubsteps = 2;
    [Tooltip("Velocity damping each substep (1=no damping).")]
    [Range(0.8f, 1f)] public float velocityDamping = 0.995f;

    [Header("GPU")]
    public ComputeShader fieldComputeShader;
    public bool useGPU = true;

    [Header("Rendering")]
    public Material fluidMaterial;
    public bool updateEveryFrame = true;
    public int updateEveryNFrames = 1;

    // internal
    private MeshFilter meshFilter;
    private Mesh mesh;
    private MarchingCubes marching = new MarchingCubes();
    private int frameCounter = 0;

    // auto-generated particle list (world space)
    private List<Vector3> generatedParticles = new List<Vector3>();
    private Bounds lastBounds;
    private bool haveLastBounds = false;

    // SPH arrays
    private Vector3[] positions;
    private Vector3[] velocities;
    private Vector3[] propulsionDirs;
    private float[] densities;
    private float[] pressures;

    // kernels constants (poly6 / spiky / viscosity) precomputed
    private float poly6Const;
    private float spikyGradConst;
    private float viscLapConst;
    private float h, h2;

    private void Reset()
    {
        room_component_safety();
    }

    private void room_component_safety()
    {
        roomComponent = GetComponentInParent<RoomComponent>();
    }

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
        if (fluidMaterial != null) mr.sharedMaterial = fluidMaterial;

        mesh = new Mesh { name = "FluidMesh" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        meshFilter.sharedMesh = mesh;

        if (roomComponent == null)
            roomComponent = GetComponentInParent<RoomComponent>();

        marching.Init(resolution);
        UpdateKernelConstants();
    }

    private void OnValidate()
    {
        if (resolution < 8) resolution = 8;
        if (smoothingLength <= 0f) smoothingLength = 0.1f;
        UpdateKernelConstants();
        if (marching != null) marching.Init(resolution);
    }

    private void Start()
    {
        RegenerateAutoIfNeeded();
        if (simulateParticles) InitSimulationIfNeeded(roomComponent?.Bounds ?? new Bounds());
    }

    private void Update()
    {
        if (!updateEveryFrame)
        {
            frameCounter++;
            if (frameCounter % updateEveryNFrames != 0) return;
        }

        if (roomComponent == null || roomComponent.Bounds.size == Vector3.zero)
            return;

        RegenerateAutoIfNeeded();

        if (simulateParticles)
        {
            float dt = Time.deltaTime / simSubsteps;
            // substeps for stability
            for (int s = 0; s < simSubsteps; s++)
                StepSimulation(dt);
        }

        GenerateAndApplyMesh();
    }

    // ------- SPH kernel constants -------
    private void UpdateKernelConstants()
    {
        h = smoothingLength;
        h2 = h * h;
        // Poly6: 315/(64*pi*h^9)
        poly6Const = 315f / (64f * Mathf.PI * Mathf.Pow(h, 9));
        // Spiky gradient: -45/(pi*h^6)
        spikyGradConst = -45f / (Mathf.PI * Mathf.Pow(h, 6));
        // Viscosity laplacian: 45/(pi*h^6)
        viscLapConst = 45f / (Mathf.PI * Mathf.Pow(h, 6));
    }

    // ------- Initialization -------
    private void InitSimulationIfNeeded(Bounds b)
    {
        // Re-init arrays when particleCount changes
        if (positions != null && positions.Length == particleCount) return;

        // Auto-tune smoothingLength and metaballRadius from room + particleCount if user left defaults
        if (roomComponent != null && smoothingLength <= 0f)
        {
            var size = b.size;
            float minDim = Mathf.Min(Mathf.Max(size.x, 0.001f), Mathf.Max(size.y, 0.001f));
            minDim = Mathf.Min(minDim, Mathf.Max(size.z, 0.001f));
            // target spacing ~ minDim / cbrt(N)
            float spacing = minDim / Mathf.Pow(Mathf.Max(1, particleCount), 1f / 3f);
            // smoothing length should be a bit larger than spacing so neighbors overlap
            smoothingLength = Mathf.Max(0.1f, spacing * 1.6f);
            Debug.Log($"[RoomFluidSimulator] Auto-tuned smoothingLength={smoothingLength:F3} spacing={spacing:F3}");
        }

        // Ensure metaball visual radius matches smoothing length for consistent surface
        metaballRadius = Mathf.Max(0.01f, smoothingLength);

        // allocate arrays
        positions = new Vector3[particleCount];
        velocities = new Vector3[particleCount];
        propulsionDirs = new Vector3[particleCount];
        densities = new float[particleCount];
        pressures = new float[particleCount];

        // Build initial positions: prefer generatedParticles (auto-population) if present
        int filled = 0;
        if (autoPopulate != AutoPopulateMode.None && generatedParticles.Count > 0)
        {
            for (int i = 0; i < generatedParticles.Count && filled < particleCount; i++)
            {
                positions[filled] = generatedParticles[i];
                velocities[filled] = Vector3.zero;
                filled++;
            }
        }

        // Fill rest randomly inside bounds (inner padded)
        Vector3 pad = b.size * autoPadding;
        Bounds inner = new Bounds(b.center, b.size - pad * 2f);
        if (inner.size.x <= 0 || inner.size.y <= 0 || inner.size.z <= 0) inner = b;

        var rnd = new System.Random(autoRandomSeed);
        while (filled < particleCount)
        {
            float rx = (float)rnd.NextDouble();
            float ry = (float)rnd.NextDouble();
            float rz = (float)rnd.NextDouble();
            positions[filled] = new Vector3(
                Mathf.Lerp(inner.min.x, inner.max.x, rx),
                Mathf.Lerp(inner.min.y, inner.max.y, ry),
                Mathf.Lerp(inner.min.z, inner.max.z, rz)
            );
            velocities[filled] = Vector3.zero;
            filled++;
        }

        // propulsion dirs
        for (int i = 0; i < particleCount; i++)
            propulsionDirs[i] = UnityEngine.Random.onUnitSphere;

        UpdateKernelConstants();
        Debug.Log($"[RoomFluidSimulator] Initialized {particleCount} particles, metaballRadius={metaballRadius:F3}, smoothingLength={smoothingLength:F3}");
    }

    // ------- Simulation Step (SPH + active) -------
    private void StepSimulation(float dt)
    {
        if (positions == null || positions.Length == 0) return;
        int N = positions.Length;

        // compute densities
        for (int i = 0; i < N; i++)
        {
            float rho = 0f;
            Vector3 xi = positions[i];
            for (int j = 0; j < N; j++)
            {
                Vector3 rij = xi - positions[j];
                float r2 = rij.sqrMagnitude;
                if (r2 >= h2) continue;
                float term = h2 - r2;
                rho += particleMass * poly6Const * term * term * term;
            }
            densities[i] = Mathf.Max(1e-6f, rho);
            pressures[i] = stiffness * Mathf.Max(0f, densities[i] - restDensity);
        }

        // compute forces
        Vector3[] forces = new Vector3[N];
        for (int i = 0; i < N; i++) forces[i] = Vector3.zero;

        for (int i = 0; i < N; i++)
        {
            Vector3 xi = positions[i];
            float pi = pressures[i];

            Vector3 fPressure = Vector3.zero;
            Vector3 fVisc = Vector3.zero;

            for (int j = 0; j < N; j++)
            {
                if (i == j) continue;
                Vector3 xj = positions[j];
                Vector3 rij = xi - xj;
                float r = rij.magnitude;
                if (r >= h || r <= 1e-6f) continue;

                // Pressure force (symmetrized)
                float pj = pressures[j];
                float rhoi_j = densities[j];
                Vector3 rhat = rij / r;
                float spiky = spikyGradConst * (h - r) * (h - r); // times rhat below
                Vector3 grad = spiky * rhat;
                fPressure += -particleMass * (pi + pj) / (2f * rhoi_j) * grad;

                // Viscosity force
                Vector3 vij = velocities[j] - velocities[i];
                float lap = viscLapConst * (h - r);
                fVisc += viscosity * particleMass * vij / rhoi_j * lap;
            }

            // external forces (gravity)
            Vector3 fExt = externalAccel * particleMass;

            // propulsion (active)
            Vector3 fProp = propulsionDirs[i] * propulsionStrength;

            forces[i] = fPressure + fVisc + fExt + fProp;
        }

        // integrate (symplectic Euler)
        for (int i = 0; i < N; i++)
        {
            Vector3 v = velocities[i];
            v += dt * (forces[i] / densities[i]);
            v *= velocityDamping;
            velocities[i] = v;
            positions[i] += dt * v;
        }

        // handle propulsion direction noise and simple alignment (optional)
        for (int i = 0; i < N; i++)
        {
            // noisy reorientation to keep activity interesting
            Vector3 n = UnityEngine.Random.onUnitSphere * propulsionNoise * Time.deltaTime;
            propulsionDirs[i] = (propulsionDirs[i] + n).normalized;
        }

        // boundary collisions (room box)
        EnforceBounds();
    }

    private void EnforceBounds()
    {
        if (roomComponent == null) return;
        Bounds b = roomComponent.Bounds;
        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 p = positions[i];
            Vector3 v = velocities[i];
            // X
            if (p.x < b.min.x)
            {
                p.x = b.min.x + 0.001f;
                v.x = -v.x * 0.5f;
            }
            else if (p.x > b.max.x)
            {
                p.x = b.max.x - 0.001f;
                v.x = -v.x * 0.5f;
            }
            // Y
            if (p.y < b.min.y)
            {
                p.y = b.min.y + 0.001f;
                v.y = -v.y * 0.5f;
            }
            else if (p.y > b.max.y)
            {
                p.y = b.max.y - 0.001f;
                v.y = -v.y * 0.5f;
            }
            // Z
            if (p.z < b.min.z)
            {
                p.z = b.min.z + 0.001f;
                v.z = -v.z * 0.5f;
            }
            else if (p.z > b.max.z)
            {
                p.z = b.max.z - 0.001f;
                v.z = -v.z * 0.5f;
            }

            positions[i] = p;
            velocities[i] = v;
        }
    }

    // ------- Surface / Marching integration (GPU or CPU) -------
    private void GenerateAndApplyMesh()
    {
        Bounds b = roomComponent.Bounds;
        Vector3 min = b.min;
        Vector3 max = b.max;

        int resCells = resolution;
        int samplesPerAxis = resCells + 1;
        Vector3 size = max - min;
        Vector3 step = new Vector3(size.x / resCells, size.y / resCells, size.z / resCells);

        float[,,] field;
        Vector3[] particles = GetActiveParticlePositions();

        if (useGPU && fieldComputeShader != null)
        {
            float[] flat = BuildFieldOnGPU(samplesPerAxis, size, b.center, step, particles);
            if (flat == null)
                field = BuildFieldOnCPU(samplesPerAxis, min, step, particles);
            else
            {
                field = new float[samplesPerAxis, samplesPerAxis, samplesPerAxis];
                int res = samplesPerAxis;
                float minVal = float.MaxValue, maxVal = float.MinValue;
                for (int x = 0; x < res; x++)
                    for (int y = 0; y < res; y++)
                        for (int z = 0; z < res; z++)
                        {
                            int idx = x * res * res + y * res + z;
                            float v = flat[idx];
                            // clamp pathological values
                            v = Mathf.Clamp(v, 0f, 10f);
                            field[x, y, z] = v;
                            if (v < minVal) minVal = v;
                            if (v > maxVal) maxVal = v;
                        }
                Debug.Log($"[RoomFluidSimulator] GPU field min={minVal:F4} max={maxVal:F4}");
                if (maxVal < 1e-4f)
                {
                    Debug.LogWarning("[RoomFluidSimulator] Field nearly zero. Increase smoothingLength / metaballRadius or particle density.");
                }
            }
        }
        else
        {
            field = BuildFieldOnCPU(samplesPerAxis, min, step, particles);
            // compute quick min/max and clamp
            float minVal = float.MaxValue, maxVal = float.MinValue;
            int res = samplesPerAxis;
            for (int x = 0; x < res; x++)
                for (int y = 0; y < res; y++)
                    for (int z = 0; z < res; z++)
                    {
                        float v = field[x, y, z];
                        v = Mathf.Clamp(v, 0f, 10f);
                        field[x, y, z] = v;
                        if (v < minVal) minVal = v;
                        if (v > maxVal) maxVal = v;
                    }
            Debug.Log($"[RoomFluidSimulator] CPU field min={minVal:F4} max={maxVal:F4}");
            if (maxVal < 1e-4f)
            {
                Debug.LogWarning("[RoomFluidSimulator] Field nearly zero. Increase smoothingLength / metaballRadius or particle density.");
            }
        }

        // log first few particle positions for debugging (helps if everything collapsed to one corner)
        if (particles != null && particles.Length > 0)
        {
            string s = $"[RoomFluidSimulator] particles[0..min(4,N)]:";
            for (int i = 0; i < Mathf.Min(4, particles.Length); i++) s += $" {particles[i]:F3}";
            Debug.Log(s);
        }

        // If field looks okay, run marching
        marching.Generate(field, isoLevel, min, step, out List<Vector3> verts, out List<int> tris, out List<Vector3> normals);

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        if (normals != null && normals.Count == verts.Count) mesh.SetNormals(normals);
        else mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private float[,,] BuildFieldOnCPU(int samplesPerAxis, Vector3 min, Vector3 step, Vector3[] particles)
    {
        int res = samplesPerAxis - 1;
        var field = new float[samplesPerAxis, samplesPerAxis, samplesPerAxis];
        for (int x = 0; x <= res; x++)
        for (int y = 0; y <= res; y++)
        for (int z = 0; z <= res; z++)
        {
            Vector3 worldPos = new Vector3(min.x + x * step.x, min.y + y * step.y, min.z + z * step.z);
            field[x, y, z] = EvaluateMetaballField(worldPos, particles);
        }
        return field;
    }

    // GPU path uses your existing compute; expects particle positions in world space (we convert inside)
    private float[] BuildFieldOnGPU(int samplesPerAxis, Vector3 size, Vector3 roomCenter, Vector3 step, Vector3[] particles)
    {
        if (fieldComputeShader == null) return null;
        int kernel = fieldComputeShader.FindKernel("BuildField");

        int res = samplesPerAxis;
        int total = res * res * res;

        ComputeBuffer particleBuffer = null;
        ComputeBuffer fieldBuffer = null;

        try
        {
            int active = particles?.Length ?? 0;
            if (active == 0) return new float[total];

            // convert to room-local (shader expects particles relative to room center)
            Vector3[] localPositions = new Vector3[active];
            for (int i = 0; i < active; i++)
                localPositions[i] = particles[i] - roomCenter;

            particleBuffer = new ComputeBuffer(active, sizeof(float) * 3);
            particleBuffer.SetData(localPositions);

            fieldBuffer = new ComputeBuffer(total, sizeof(float));
            fieldBuffer.SetData(new float[total]);

            fieldComputeShader.SetBuffer(kernel, "_Particles", particleBuffer);
            fieldComputeShader.SetInt("_ParticleCount", active);
            fieldComputeShader.SetBuffer(kernel, "_Field", fieldBuffer);

            fieldComputeShader.SetInt("_Resolution", res);
            fieldComputeShader.SetVector("_Size", new Vector4(size.x, size.y, size.z, 0f));
            fieldComputeShader.SetFloat("_ParticleRadius", metaballRadius);

            int tgX = Mathf.CeilToInt(res / 8.0f);
            int tgY = Mathf.CeilToInt(res / 8.0f);
            int tgZ = Mathf.CeilToInt(res / 8.0f);
            fieldComputeShader.Dispatch(kernel, tgX, tgY, tgZ);

            float[] flat = new float[total];
            fieldBuffer.GetData(flat);
            return flat;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[RoomFluidSimulator] GPU field build failed: " + e.Message);
            return null;
        }
        finally
        {
            if (particleBuffer != null) { particleBuffer.Release(); particleBuffer = null; }
            if (fieldBuffer != null) { fieldBuffer.Release(); fieldBuffer = null; }
        }
    }

    // metaball bounded cubic kernel used for surface rendering
    private float EvaluateMetaballField(Vector3 worldPos, Vector3[] particles)
    {
        if (particles == null || particles.Length == 0) return 0f;
        float value = 0f;
        float r = metaballRadius;
        float rInv = 1f / Mathf.Max(1e-6f, r);
        for (int i = 0; i < particles.Length; i++)
        {
            float d = Vector3.Distance(worldPos, particles[i]);
            if (d >= r) continue;
            float u = d * rInv;
            float contrib = (1f - u);
            value += contrib * contrib * contrib; // (1-u)^3
        }
        return value;
    }

    // active source for rendering: prefer simulated positions when running
    private Vector3[] GetActiveParticlePositions()
    {
        if (simulateParticles && positions != null && positions.Length > 0)
            return positions;
        if (autoPopulate != AutoPopulateMode.None && generatedParticles.Count > 0)
            return generatedParticles.ToArray();
        return Array.Empty<Vector3>();
    }

    // ------- Auto generation -------
    private void RegenerateAutoIfNeeded()
    {
        if (autoPopulate == AutoPopulateMode.None) return;
        Bounds b = roomComponent.Bounds;
        if (!haveLastBounds || autoRegenerateOnBoundsChange && (b.center != lastBounds.center || b.size != lastBounds.size))
        {
            GenerateAutoParticles(b);
            lastBounds = b;
            haveLastBounds = true;
        }
    }

    private void GenerateAutoParticles(Bounds b)
    {
        generatedParticles.Clear();
        Vector3 pad = b.size * autoPadding;
        Bounds inner = new Bounds(b.center, b.size - pad * 2f);
        if (inner.size.x <= 0 || inner.size.y <= 0 || inner.size.z <= 0) inner = b;

        if (autoPopulate == AutoPopulateMode.Grid)
        {
            int n = Mathf.CeilToInt(Mathf.Pow(Mathf.Max(1, autoCount), 1f / 3f));
            if (n < 1) n = 1;
            for (int ix = 0; ix < n; ix++)
                for (int iy = 0; iy < n; iy++)
                    for (int iz = 0; iz < n; iz++)
                    {
                        if (generatedParticles.Count >= autoCount) break;
                        float fx = n == 1 ? 0.5f : (float)ix / (n - 1);
                        float fy = n == 1 ? 0.5f : (float)iy / (n - 1);
                        float fz = n == 1 ? 0.5f : (float)iz / (n - 1);
                        Vector3 p = new Vector3(
                            Mathf.Lerp(inner.min.x, inner.max.x, fx),
                            Mathf.Lerp(inner.min.y, inner.max.y, fy),
                            Mathf.Lerp(inner.min.z, inner.max.z, fz)
                        );
                        generatedParticles.Add(p);
                    }
        }
        else if (autoPopulate == AutoPopulateMode.Random)
        {
            var rnd = new System.Random(autoRandomSeed);
            for (int i = 0; i < autoCount; i++)
            {
                float rx = (float)rnd.NextDouble();
                float ry = (float)rnd.NextDouble();
                float rz = (float)rnd.NextDouble();
                Vector3 p = new Vector3(
                    Mathf.Lerp(inner.min.x, inner.max.x, rx),
                    Mathf.Lerp(inner.min.y, inner.max.y, ry),
                    Mathf.Lerp(inner.min.z, inner.max.z, rz)
                );
                generatedParticles.Add(p);
            }
        }

        // ensure sim arrays re-init if we're simulating particles from auto-generation
        if (simulateParticles)
            InitSimulationIfNeeded(b);

        Debug.Log($"[RoomFluidSimulator] Generated {generatedParticles.Count} particles (mode={autoPopulate}).");
    }
}