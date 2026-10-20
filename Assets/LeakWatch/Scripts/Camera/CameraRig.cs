using UnityEngine;

namespace LeakWatch
{
    public struct Shot
    {
        public Vector3 subject;
        public float yaw, pitch, dist;
        public Vector2 shift;

        public Shot(Vector3 subject, float yaw, float pitch, float dist, Vector2 shift)
        {
            this.subject = subject; this.yaw = yaw; this.pitch = pitch; this.dist = dist; this.shift = shift;
        }
    }

    /// Smooth cinematic camera. `shift` moves the subject on screen (fraction of half-width/height)
    /// so it stays clear of the dashboard and analysis panel.
    public class CameraRig : MonoBehaviour
    {
        Shot target;
        Vector3 subject; Vector3 subjectVel;
        float yaw, yawVel, pitch, pitchVel, dist, distVel;
        Vector2 shift, shiftVel;
        bool hasShot;
        const float SmoothTime = 1.15f;

        public void SetShot(Shot s, bool snap)
        {
            target = s;
            if (!hasShot || snap)
            {
                subject = s.subject; yaw = s.yaw; pitch = s.pitch; dist = s.dist; shift = s.shift;
                subjectVel = Vector3.zero; yawVel = pitchVel = distVel = 0f; shiftVel = Vector2.zero;
                hasShot = true;
            }
        }

        void LateUpdate()
        {
            if (!hasShot) return;
            float dt = Time.unscaledDeltaTime;
            subject = Vector3.SmoothDamp(subject, target.subject, ref subjectVel, SmoothTime, Mathf.Infinity, dt);
            yaw = Mathf.SmoothDampAngle(yaw, target.yaw, ref yawVel, SmoothTime, Mathf.Infinity, dt);
            pitch = Mathf.SmoothDamp(pitch, target.pitch, ref pitchVel, SmoothTime, Mathf.Infinity, dt);
            dist = Mathf.SmoothDamp(dist, target.dist, ref distVel, SmoothTime, Mathf.Infinity, dt);
            shift = Vector2.SmoothDamp(shift, target.shift, ref shiftVel, SmoothTime, Mathf.Infinity, dt);

            var cam = Ctx.Cam;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            float halfH = dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfW = halfH * cam.aspect;
            Vector3 pivot = subject - rot * Vector3.right * (shift.x * halfW) - rot * Vector3.up * (shift.y * halfH);
            cam.transform.rotation = rot;
            cam.transform.position = pivot - rot * Vector3.forward * dist;
        }
    }
}
