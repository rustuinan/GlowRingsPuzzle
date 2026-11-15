using UnityEngine;
using UnityEngine.UI;

/// <summary>One UI mesh for a short gold burst, sparkles and falling confetti.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class DailySpinBurstGraphic : MaskableGraphic
{
    private struct Particle
    { public Vector2 Velocity; public float Delay, Life, Gravity, Size, Spin, Angle; public Color Color; }
    private Particle[] particles;
    private Vector2 origin;
    private float age = 10;
    private static readonly Color[] Colors = { new Color32(255, 212, 102, 255), new Color32(116, 234, 255, 255),
        new Color32(199, 128, 255, 255), new Color32(255, 124, 184, 255), new Color32(255, 243, 197, 255) };
    public void Initialize(Vector2 center, int seed)
    {
        origin = center; age = 0; raycastTarget = false;
        var random = new System.Random(seed); particles = new Particle[64];
        for (int i = 0; i < particles.Length; i++)
            particles[i] = new Particle {
                Velocity = new Vector2(Range(random, -440, 440), Range(random, 180, 710)),
                Delay = Range(random, 0, 0.22f), Life = Range(random, 2.2f, 3.2f), Gravity = Range(random, 520, 800),
                Size = Range(random, 7, 15), Spin = Range(random, -300, 300), Angle = Range(random, 0, 360), Color = Colors[i % Colors.Length] };
        SetVerticesDirty();
    }
    public void SetTime(float time) { age = time; SetVerticesDirty(); }
    public void Clear() { age = 10; particles = null; SetVerticesDirty(); }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear(); if (particles == null) return;
        float pulse = Mathf.Clamp01(age / 0.85f);
        if (pulse < 1)
        {
            float r = Mathf.Lerp(55, 510, pulse), alpha = (1 - pulse) * (1 - pulse);
            Ring(mesh, origin, r, r + 20, new Color(1, 0.78f, 0.28f, alpha * 0.30f));
            Ring(mesh, origin, r * 0.75f, r * 0.75f + 4, new Color(0.66f, 0.93f, 1, alpha * 0.35f));
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2 / 12 + age * 0.15f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 side = new Vector2(-direction.y, direction.x);
                int start = mesh.currentVertCount; Color color = new Color(1, 0.83f, 0.42f, alpha * 0.14f);
                mesh.AddVert(origin + direction * 70 - side * 4, color, Vector2.zero);
                mesh.AddVert(origin + direction * 70 + side * 4, color, Vector2.zero);
                mesh.AddVert(origin + direction * (220 + pulse * 310), new Color(1, 0.85f, 0.5f, 0), Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
            }
        }
        foreach (var particle in particles)
        {
            float time = age - particle.Delay; if (time < 0 || time > particle.Life) continue;
            Vector2 position = origin + particle.Velocity * time + Vector2.down * particle.Gravity * time * time * 0.5f;
            position.x += Mathf.Sin(time * 5 + particle.Angle) * time * 12;
            if (Mathf.Abs(position.x) > 565 || position.y < -950 || position.y > 955) continue;
            Color color = particle.Color; color.a *= Mathf.Clamp01((particle.Life - time) / 0.65f);
            float angle = (particle.Angle + particle.Spin * time) * Mathf.Deg2Rad;
            Vector2 right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * particle.Size * (0.25f + Mathf.Abs(Mathf.Cos(time * 6)) * 0.75f);
            Vector2 up = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * particle.Size * 0.65f;
            Quad(mesh, position - right - up, position - right + up, position + right + up, position + right - up, color);
        }
        for (int i = 0; i < 10; i++)
        {
            float time = age - i * 0.035f; if (time < 0 || time > 1.35f) continue;
            float angle = i * Mathf.PI * 2 / 10 + 0.3f;
            Vector2 position = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (110 + time * 190);
            Color color = Colors[i % Colors.Length]; color.a *= 1 - time / 1.35f;
            Star(mesh, position, Mathf.Sin(Mathf.Clamp01(time / 0.5f) * Mathf.PI) * 17 + 3, color);
        }
    }
    private static void Star(VertexHelper mesh, Vector2 center, float radius, Color color)
    {
        int start = mesh.currentVertCount; mesh.AddVert(center, color, Vector2.zero);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4; float r = i % 2 == 0 ? radius : radius * 0.22f;
            mesh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, color, Vector2.zero);
        }
        for (int i = 0; i < 8; i++) mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 8);
    }
    private static void Ring(VertexHelper mesh, Vector2 center, float inner, float outer, Color color)
    {
        for (int i = 0; i < 64; i++)
        {
            float a = i * Mathf.PI * 2 / 64, b = (i + 1) * Mathf.PI * 2 / 64;
            Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), q = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            Quad(mesh, center + p * inner, center + q * inner, center + q * outer, center + p * outer, color);
        }
    }
    private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
    {
        int start = mesh.currentVertCount; mesh.AddVert(a, color, Vector2.zero); mesh.AddVert(b, color, Vector2.zero);
        mesh.AddVert(c, color, Vector2.zero); mesh.AddVert(d, color, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
    }
    private static float Range(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
}
