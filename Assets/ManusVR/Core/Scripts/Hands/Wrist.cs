// Copyright (c) 2018 ManusVR

using ManusVR.Core.Apollo;
using UnityEngine;

namespace ManusVR.Core.Hands
{
    public class Wrist : MonoBehaviour
    {
        public device_type_t DeviceType { get; set; }
        public Hand Hand { get; set; }
        public Rigidbody Rigidbody { get; private set; }

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            if (Rigidbody == null)
            {
                Rigidbody = gameObject.AddComponent<Rigidbody>();
            }
            Rigidbody.centerOfMass = Vector3.zero;
        }

        public virtual void Start()
        {
            Rigidbody.isKinematic = true;
        }

        /// <summary>
        /// Rotate the wrist towards the given rotation
        /// </summary>
        /// <param name="rotation"></param>
        public virtual void RotateWrist(Quaternion rotation)
        {
            // note Quaternion(float x, float y, float z, float w);
            // note this not a local rotation but a rotation relative to the world coordinate system
            transform.rotation = rotation;
        }
    }
}
