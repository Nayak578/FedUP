using System.Collections.Generic;
using UnityEngine;

namespace FedUp.Ghost.Scares
{
    /// <summary>
    /// A working mirror: a second camera at your reflected position renders into the mirror's texture (the
    /// material flips it left-right). It can show the reflection a little late (<see cref="Delay"/>), the GDD's
    /// "reflection slightly delayed". The greybox player has no body, so a stand-in body that only the mirror can
    /// see stands where you are; the delay makes that reflection follow you a beat late. Only renders while
    /// you're near it and in front of it.
    /// </summary>
    public class MirrorView : MonoBehaviour
    {
        [SerializeField] Camera viewer;
        [SerializeField] Camera mirrorCamera;
        [SerializeField, Min(1f)] float renderWithin = 7f;
        [Tooltip("Your stand-in body, on the mirror-only layer.")]
        [SerializeField] Transform reflectionBody;
        [SerializeField] float eyeHeight = 1.6f;

        /// <summary>Seconds the reflection lags behind you (0 = a normal mirror).</summary>
        public float Delay { get; set; }

        readonly Queue<(float t, Vector3 p, Quaternion r)> history = new Queue<(float, Vector3, Quaternion)>();

        Vector3 Normal => transform.forward;   // the mirror faces this way, into the room
        Vector3 Plane => transform.position;

        void LateUpdate()
        {
            Transform v = viewer.transform;
            history.Enqueue((Time.time, v.position, v.rotation));
            while (history.Count > 2 && history.Peek().t < Time.time - 2f) history.Dequeue();

            bool inFront = Vector3.Dot(v.position - Plane, Normal) > 0.05f;
            bool near = (v.position - Plane).sqrMagnitude < renderWithin * renderWithin;
            mirrorCamera.enabled = inFront && near;
            if (!mirrorCamera.enabled) return;

            // your reflection follows you Delay seconds late
            if (reflectionBody != null)
            {
                var (bp, br) = Sample(Time.time - Delay);
                Vector3 fwd = br * Vector3.forward; fwd.y = 0f;
                reflectionBody.SetPositionAndRotation(new Vector3(bp.x, bp.y - eyeHeight, bp.z),
                    fwd.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(fwd) : Quaternion.identity);
            }
            // the mirror itself shows the room as it is now: reflect the viewer through the mirror plane
            Vector3 pos = v.position;
            Vector3 rp = pos - 2f * Vector3.Dot(pos - Plane, Normal) * Normal;
            Vector3 f = Vector3.Reflect(v.forward, Normal);
            Vector3 u = Vector3.Reflect(v.up, Normal);
            mirrorCamera.transform.SetPositionAndRotation(rp, Quaternion.LookRotation(f, u));
            mirrorCamera.fieldOfView = viewer.fieldOfView;
            mirrorCamera.ResetProjectionMatrix();
            // clip everything behind the mirror (the wall it hangs on)
            Matrix4x4 w2c = mirrorCamera.worldToCameraMatrix;
            Vector3 cp = w2c.MultiplyPoint(Plane + Normal * 0.02f);
            Vector3 cn = w2c.MultiplyVector(Normal).normalized;
            mirrorCamera.projectionMatrix = mirrorCamera.CalculateObliqueMatrix(new Vector4(cn.x, cn.y, cn.z, -Vector3.Dot(cp, cn)));
        }

        (Vector3, Quaternion) Sample(float t)
        {
            // the newest pose at or before t (or the oldest we have)
            if (history.Count == 0) return (viewer.transform.position, viewer.transform.rotation);
            var best = history.Peek();
            foreach (var h in history)
            {
                if (h.t > t) break;
                best = h;
            }
            return (best.p, best.r);
        }
    }
}
