using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight, code-built shooting effects: bullet tracer, impact sparks and pooled bullet-hole decals.
/// Used by <see cref="Shoot"/> when no prefab is assigned for those slots.
/// </summary>
public static class WeaponFX
{
    private static Material lineMat, sparkMat, decalMat;
    private static readonly Queue<GameObject> decals = new Queue<GameObject>();
    private const int MaxDecals = 40;
    private static FXRunner runner;

    private static Shader FindShader(params string[] names)
    {
        foreach (var n in names) { var s = Shader.Find(n); if (s != null) return s; }
        return Shader.Find("Sprites/Default");
    }

    private static FXRunner Runner
    {
        get
        {
            if (runner == null)
            {
                var go = new GameObject("WeaponFX");
                runner = go.AddComponent<FXRunner>();
            }
            return runner;
        }
    }

    // ---------------------------------------------------------------- tracer
    public static void SpawnTracer(Vector3 from, Vector3 to)
    {
        if (lineMat == null) lineMat = new Material(FindShader("Sprites/Default"));
        var go = new GameObject("Tracer");
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = lineMat;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.numCapVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        Runner.StartCoroutine(Tracer(lr, from, to));
    }

    private static IEnumerator Tracer(LineRenderer lr, Vector3 from, Vector3 to)
    {
        // A short bright streak that races from the muzzle to the hit point and fades.
        float dist = Vector3.Distance(from, to);
        const float speed = 260f, streak = 9f;
        float t = 0f;
        Color hot = new Color(1f, 0.85f, 0.55f, 1f);
        while (lr != null)
        {
            t += Time.deltaTime;
            float head = Mathf.Min(dist, t * speed);
            float tail = Mathf.Max(0f, head - streak);
            Vector3 dir = dist > 0.001f ? (to - from) / dist : Vector3.forward;
            lr.SetPosition(0, from + dir * tail);
            lr.SetPosition(1, from + dir * head);
            float fade = head >= dist ? Mathf.Clamp01(1f - (t - dist / speed) / 0.05f) : 1f;
            lr.startColor = new Color(hot.r, hot.g, hot.b, 0f);
            lr.endColor = new Color(hot.r, hot.g, hot.b, fade);
            lr.startWidth = 0.012f;
            lr.endWidth = 0.04f;
            if (fade <= 0f) break;
            yield return null;
        }
        if (lr != null) Object.Destroy(lr.gameObject);
    }

    // ---------------------------------------------------------------- sparks
    public static void SpawnImpactSparks(Vector3 point, Vector3 normal)
    {
        if (sparkMat == null)
        {
            sparkMat = new Material(FindShader("Universal Render Pipeline/Particles/Unlit", "Sprites/Default"));
            if (sparkMat.HasProperty("_Surface")) { sparkMat.SetFloat("_Surface", 1f); sparkMat.SetFloat("_Blend", 2f); } // transparent additive
            sparkMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            sparkMat.SetColor("_BaseColor", Color.white);
        }
        var go = new GameObject("ImpactSparks");
        go.transform.SetPositionAndRotation(point + normal * 0.02f, Quaternion.LookRotation(normal));
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.45f), new Color(1f, 0.55f, 0.2f));
        main.gravityModifier = 1.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)10, (short)18) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.01f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = sparkMat;
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.lengthScale = 2.5f;
        r.velocityScale = 0.04f;

        // A puff of grey dust as well.
        var dust = new GameObject("ImpactDust").AddComponent<ParticleSystem>();
        dust.transform.SetPositionAndRotation(go.transform.position, go.transform.rotation);
        dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var dm = dust.main;
        dm.duration = 0.5f; dm.loop = false;
        dm.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        dm.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        dm.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        dm.startColor = new Color(0.55f, 0.53f, 0.5f, 0.35f);
        dm.simulationSpace = ParticleSystemSimulationSpace.World;
        dm.stopAction = ParticleSystemStopAction.Destroy;
        var de = dust.emission; de.rateOverTime = 0f; de.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)4, (short)7) });
        var dsh = dust.shape; dsh.shapeType = ParticleSystemShapeType.Cone; dsh.angle = 25f; dsh.radius = 0.02f;
        var col = dust.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var dr = dust.GetComponent<ParticleSystemRenderer>();
        dr.sharedMaterial = lineMat != null ? lineMat : (lineMat = new Material(FindShader("Sprites/Default")));

        ps.Play();
        dust.Play();
    }

    // ---------------------------------------------------------------- decals
    public static void SpawnBulletHole(RaycastHit hit)
    {
        if (decalMat == null)
        {
            decalMat = new Material(FindShader("Sprites/Default"));
            var tex = Resources.Load<Texture2D>("OIP-removebg-preview"); // Assets/Prefab/Resources
            if (tex != null) decalMat.mainTexture = tex;
            decalMat.color = new Color(0.25f, 0.22f, 0.2f, 0.95f);
        }
        GameObject quad;
        if (decals.Count >= MaxDecals)
        {
            quad = decals.Dequeue();
            if (quad == null) quad = NewDecal();
        }
        else quad = NewDecal();

        quad.SetActive(true);
        quad.transform.SetParent(null, false);
        quad.transform.SetPositionAndRotation(hit.point + hit.normal * 0.008f, Quaternion.LookRotation(-hit.normal));
        quad.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
        quad.transform.localScale = Vector3.one * Random.Range(0.07f, 0.11f);
        if (hit.collider != null && (hit.rigidbody == null || hit.rigidbody.isKinematic))
            quad.transform.SetParent(hit.collider.transform, true);
        decals.Enqueue(quad);
    }

    private static GameObject NewDecal()
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "BulletHole";
        Object.Destroy(quad.GetComponent<Collider>());
        var r = quad.GetComponent<MeshRenderer>();
        r.sharedMaterial = decalMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return quad;
    }

    private class FXRunner : MonoBehaviour { }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        decals.Clear();
        runner = null;
    }
}
