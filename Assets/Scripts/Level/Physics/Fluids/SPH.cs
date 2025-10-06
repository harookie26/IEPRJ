using System.Collections.Generic;
using UnityEngine;

public class SPH : MonoBehaviour
{
    [Header("Simulation")]
    [Range(32, 5000)] public int particleCount = 1000;
    [Tooltip("Fixed simulation step (s).")] public float timeStep = 0.005f;
    [Tooltip("Substeps per frame for stability (1–4).")] public int substeps = 1;
    public Vector3 gravity = new Vector3(0, -9.81f, 0);

    [Header("Fluid Parameters")]
    public float restDensity = 1000f;     // kg/m^3
    public float gasConstant = 2000f;     // Stiffness (k)
    public float viscosity = 0.1f;        // Dynamic viscosity term
    public float particleMass = 0.02f;    // Mass per particle
    public float smoothingRadius = 0.1f;  // Kernel radius h

    [Header("Initial Volume Spawn (Local Space)")]
    public Vector3 spawnCenter = Vector3.zero;
    public Vector3 spawnSize = new Vector3(0.6f, 0.6f, 0.6f);

    [Header("Bounds (World Space)")]
    public Vector3 boundsMin = new Vector3(-1, 0, -1);
    public Vector3 boundsMax = new Vector3(1, 2, 1);
    [Range(0f, 1f)] public float boundaryDamping = 0.5f;

    [Header("Rendering")]
    public Mesh particleMesh;
    public Material particleMaterial;
    public float particleRenderScale = 0.05f;

    [Header("Debug / Gizmos")]
    public bool showBounds = true;
    public bool showSpawnVolume = true;
    public bool showFirstParticleRadius = false;
    public Color boundsColor = new Color(0f, 1f, 1f, 1f);
    public Color spawnColor = new Color(1f, 0.65f, 0f, 1f);
    public Color radiusColor = new Color(0.2f, 0.6f, 1f, 0.35f);
    public bool alwaysShowGizmos = true; // If false, only when selected

    // Internal particle representation
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 force;
        public float density;
        public float pressure;
    }

    List<Particle> _particles;
    Matrix4x4[] _matrices = new Matrix4x4[1023]; // batch buffer

    // Kernel precomputed constants
    float _poly6Const;
    float _spikyGradConst;
    float _viscLaplacianConst;
    float _h2;
    float _h;

    float _accumulator;

    void Awake()
    {
        InitializeKernels();
        InitializeParticles();
    }

    void InitializeKernels()
    {
        _h = smoothingRadius;
        _h2 = _h * _h;

        float h9 = Mathf.Pow(_h, 9);
        float h6 = Mathf.Pow(_h, 6);

        _poly6Const = 315f / (64f * Mathf.PI * h9);
        _spikyGradConst = -45f / (Mathf.PI * h6);
        _viscLaplacianConst = 45f / (Mathf.PI * h6);
    }

    void InitializeParticles()
    {
        _particles = new List<Particle>(particleCount);

        int perAxis = Mathf.CeilToInt(Mathf.Pow(particleCount, 1f / 3f));
        Vector3 step = new Vector3(
            spawnSize.x / perAxis,
            spawnSize.y / perAxis,
            spawnSize.z / perAxis);

        int created = 0;
        for (int x = 0; x < perAxis && created < particleCount; x++)
        {
            for (int y = 0; y < perAxis && created < particleCount; y++)
            {
                for (int z = 0; z < perAxis && created < particleCount; z++)
                {
                    Vector3 local = spawnCenter +
                                    new Vector3(
                                        (x + 0.5f) * step.x - spawnSize.x * 0.5f,
                                        (y + 0.5f) * step.y - spawnSize.y * 0.5f,
                                        (z + 0.5f) * step.z - spawnSize.z * 0.5f);

                    Particle p = new Particle
                    {
                        position = transform.TransformPoint(local),
                        velocity = Vector3.zero,
                        force = Vector3.zero,
                        density = restDensity,
                        pressure = 0
                    };
                    _particles.Add(p);
                    created++;
                }
            }
        }
    }

    void Update()
    {
        _accumulator += Time.deltaTime;
        float dt = timeStep;

        int steps = 0;
        while (_accumulator >= dt && steps < substeps)
        {
            Step(dt);
            _accumulator -= dt;
            steps++;
        }

        RenderParticles();
    }

    void Step(float dt)
    {
        ComputeDensityPressure();
        ComputeForces();
        Integrate(dt);
        HandleBoundaries();
    }

    void ComputeDensityPressure()
    {
        int count = _particles.Count;
        for (int i = 0; i < count; i++)
        {
            Particle pi = _particles[i];
            float density = 0f;

            for (int j = 0; j < count; j++)
            {
                Vector3 rij = pi.position - _particles[j].position;
                float r2 = rij.sqrMagnitude;
                if (r2 < _h2)
                {
                    float term = _h2 - r2;
                    density += particleMass * _poly6Const * term * term * term;
                }
            }

            pi.density = Mathf.Max(density, restDensity * 0.5f);
            pi.pressure = gasConstant * (pi.density - restDensity);
            _particles[i] = pi;
        }
    }

    void ComputeForces()
    {
        int count = _particles.Count;

        for (int i = 0; i < count; i++)
        {
            Particle pi = _particles[i];
            Vector3 pressureForce = Vector3.zero;
            Vector3 viscosityForce = Vector3.zero;

            for (int j = 0; j < count; j++)
            {
                if (i == j) continue;

                Particle pj = _particles[j];
                Vector3 rij = pi.position - pj.position;
                float r = rij.magnitude;
                if (r < _h && r > 0f)
                {
                    Vector3 grad = SpikyGradient(rij, r);
                    pressureForce += -grad * particleMass * ((pi.pressure + pj.pressure) / (2f * pj.density));

                    float viscLap = ViscLaplacian(r);
                    viscosityForce += viscosity * particleMass * (pj.velocity - pi.velocity) / pj.density * viscLap;
                }
            }

            Vector3 gravityForce = gravity * pi.density;
            pi.force = pressureForce + viscosityForce + gravityForce;
            _particles[i] = pi;
        }
    }

    void Integrate(float dt)
    {
        for (int i = 0; i < _particles.Count; i++)
        {
            Particle p = _particles[i];
            Vector3 accel = p.force / p.density;
            p.velocity += accel * dt;
            p.position += p.velocity * dt;
            _particles[i] = p;
        }
    }

    void HandleBoundaries()
    {
        for (int i = 0; i < _particles.Count; i++)
        {
            Particle p = _particles[i];
            Vector3 pos = p.position;
            Vector3 vel = p.velocity;

            if (pos.x < boundsMin.x)
            {
                pos.x = boundsMin.x;
                if (vel.x < 0) vel.x *= -boundaryDamping;
            }
            else if (pos.x > boundsMax.x)
            {
                pos.x = boundsMax.x;
                if (vel.x > 0) vel.x *= -boundaryDamping;
            }

            if (pos.y < boundsMin.y)
            {
                pos.y = boundsMin.y;
                if (vel.y < 0) vel.y *= -boundaryDamping;
            }
            else if (pos.y > boundsMax.y)
            {
                pos.y = boundsMax.y;
                if (vel.y > 0) vel.y *= -boundaryDamping;
            }

            if (pos.z < boundsMin.z)
            {
                pos.z = boundsMin.z;
                if (vel.z < 0) vel.z *= -boundaryDamping;
            }
            else if (pos.z > boundsMax.z)
            {
                pos.z = boundsMax.z;
                if (vel.z > 0) vel.z *= -boundaryDamping;
            }

            p.position = pos;
            p.velocity = vel;
            _particles[i] = p;
        }
    }

    void RenderParticles()
    {
        if (particleMesh == null || particleMaterial == null)
            return;

        int count = _particles.Count;
        int idx = 0;
        while (idx < count)
        {
            int batch = Mathf.Min(1023, count - idx);
            for (int i = 0; i < batch; i++)
            {
                Vector3 pos = _particles[idx + i].position;
                _matrices[i] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * particleRenderScale);
            }
            Graphics.DrawMeshInstanced(particleMesh, 0, particleMaterial, _matrices, batch, null,
                UnityEngine.Rendering.ShadowCastingMode.Off, false);
            idx += batch;
        }
    }

    // Kernel functions
    Vector3 SpikyGradient(Vector3 rij, float r)
    {
        float term = (_h - r);
        float scale = _spikyGradConst * term * term / r;
        return rij * scale;
    }

    float ViscLaplacian(float r)
    {
        return _viscLaplacianConst * (_h - r);
    }

    public IReadOnlyList<Vector3> GetParticlePositions()
    {
        _tempPositions.Clear();
        for (int i = 0; i < _particles.Count; i++)
            _tempPositions.Add(_particles[i].position);
        return _tempPositions;
    }
    List<Vector3> _tempPositions = new List<Vector3>();

    // Gizmo drawing
    void OnDrawGizmos()
    {
        if (!alwaysShowGizmos) return;
        DrawGizmosInternal();
    }

    void OnDrawGizmosSelected()
    {
        if (alwaysShowGizmos) return; // avoid double drawing
        DrawGizmosInternal();
    }

    void DrawGizmosInternal()
    {
        // Bounds (world)
        if (showBounds)
        {
            Gizmos.color = boundsColor;
            Vector3 size = boundsMax - boundsMin;
            Vector3 center = (boundsMax + boundsMin) * 0.5f;
            Gizmos.DrawWireCube(center, size);
        }

        // Spawn volume (local space)
        if (showSpawnVolume)
        {
            Gizmos.color = spawnColor;
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(spawnCenter, spawnSize);
            Gizmos.matrix = prev;
        }

        // First particle smoothing radius (helps tuning)
        if (showFirstParticleRadius && _particles != null && _particles.Count > 0)
        {
            Gizmos.color = radiusColor;
            Gizmos.DrawWireSphere(_particles[0].position, smoothingRadius);
        }
    }
}