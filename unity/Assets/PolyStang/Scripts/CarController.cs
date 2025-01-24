using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using EmotivUnityPlugin;

namespace PolyStang
{
    public class CarController : MonoBehaviour
    {
        public enum ControlMode //added EEG control mode
        {
            Keyboard,
            Buttons,
            EEG
        };

        public enum Axel 
        {
            Front,
            Rear
        }

        [Serializable]
        public struct Wheel 
        {
            public GameObject wheelModel;
            public WheelCollider wheelCollider;
            public GameObject wheelEffectObj;
            public ParticleSystem smokeParticle;
            public Axel axel;
            public GameObject skidSound;
            public int index;
        }

        public ControlMode control;

        [Header("Inputs")]
        public KeyCode brakeKey = KeyCode.Space;

        [Header("Accelerations and deaccelerations")]
        public float maxAcceleration = 30.0f;
        public float brakeAcceleration = 50.0f;
        public float noInputDeacceleration = 10.0f;

        [Header("Steering")]
        public float turnSensitivity = 1.0f;
        public float maxSteerAngle = 30.0f;

        [Header("Speed UI")]
        public TMP_Text speedText;
        public float UISpeedMultiplier = 4;

        [Header("Speed limit")]
        public float frontMaxSpeed = 200;
        public float rearMaxSpeed = 50;
        public float empiricalCoefficient = 0.41f;
        public enum TypeOfSpeedLimit
        {
            noSpeedLimit,
            simple,
            squareRoot
        };
        public TypeOfSpeedLimit typeOfSpeedLimit = TypeOfSpeedLimit.squareRoot;
        private float frontSpeedReducer = 1;
        private float rearSpeedReducer = 1;

        [Header("Skid")]
        public float brakeDriftingSkidLimit = 10f;
        public float lateralFrontDriftingSkidLimit = 0.6f;
        public float lateralRearDriftingSkidLimit = 0.3f;

        [Header("General")]
        public Vector3 _centerOfMass;

        public List<Wheel> wheels;

        float moveInput;
        float steerInput;

        private Rigidbody carRb;

        private CarLights carLights;
        private CarSounds carSounds;

        EmotivUnityItf _eItf = EmotivUnityItf.Instance;

        void Start() 
        {
            carRb = GetComponent<Rigidbody>();
            carRb.centerOfMass = _centerOfMass;

            carLights = GetComponent<CarLights>();
            carSounds = GetComponent<CarSounds>();
        }

        void Update() 
        {
            GetInputs();
            AnimateWheels();
            WheelEffectsCheck();
            CarLightsControl();
        }

        void LateUpdate() 
        {
            Move();
            Steer();
            BrakeAndDeacceleration();
            UpdateSpeedUI();
        }

        public void MoveInput(float input) 
        {
            moveInput = input;
        }

        public void SteerInput(float input) 
        {
            steerInput = input;
        }

        void GetInputs() 
        {
            if (control == ControlMode.Keyboard)
            {
                moveInput = Input.GetAxis("Vertical");
                steerInput = Input.GetAxis("Horizontal");
            }
            else if (control == ControlMode.EEG) //added headset control mode
            {
                
                //Facial Expression controls:
                if(_eItf.UPow > 0.3 || _eItf.LPow > 0.3)
                {
                    switch (_eItf.UAct)
                    {
                        case "surprise":
                            steerInput = -1; // Left
                            break;
                        case "frown":
                            steerInput = 1; // Right
                            break;
                        default:
                            steerInput = 0;
                            break;
                    }
                    switch (_eItf.LAct)
                    {
                        case "smile":
                            moveInput = 1; // Forward
                            break;
                        case "clench":
                            moveInput = -1; // Backward
                            break;
                        default:
                            moveInput = 0;
                            break;
                    }
                }
                //Mental Command controls:
                else if (_eItf.LatestMentalCommand.pow > 0.3)
                {
                    switch (_eItf.LatestMentalCommand.act)
                    {
                        case "push":
                            moveInput = 1; // Forward
                            break;
                        case "pull":
                            moveInput = -1; // Backward
                            break;
                        case "left":
                            steerInput = -1; // Left
                            moveInput = 1; // Forward
                            break;
                        case "right":
                            steerInput = 1; // Right
                            moveInput = 1; // Forward
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    moveInput = 0; //stop input when nothing is detected
                    steerInput = 0;
                }
            }
        }

        void Move() 
        {
            foreach (var wheel in wheels)
            {
               
                float currentWheelSpeed = empiricalCoefficient * wheel.wheelCollider.radius * wheel.wheelCollider.rpm;

                if (moveInput > 0 || currentWheelSpeed > 0) 
                { 
                    if(currentWheelSpeed > frontMaxSpeed) 
                    {
                        currentWheelSpeed = frontMaxSpeed;
                    }
                    
                  
                    if (typeOfSpeedLimit == TypeOfSpeedLimit.noSpeedLimit)
                    {
                        frontSpeedReducer = 1;
                    }
                    else if (typeOfSpeedLimit == TypeOfSpeedLimit.simple)
                    {
                        frontSpeedReducer = (frontMaxSpeed - currentWheelSpeed ) / frontMaxSpeed;
                    }
                    else if (typeOfSpeedLimit == TypeOfSpeedLimit.squareRoot)
                    {
                        frontSpeedReducer = Mathf.Sqrt(Mathf.Abs((frontMaxSpeed - currentWheelSpeed) / frontMaxSpeed));
                    }

                    
                    wheel.wheelCollider.motorTorque = moveInput * 600 * maxAcceleration * frontSpeedReducer * Time.deltaTime;
                }
                else if (moveInput < 0 || currentWheelSpeed < 0) 
                {
                    if (currentWheelSpeed < - rearMaxSpeed) 
                    {
                        currentWheelSpeed = - rearMaxSpeed;
                    }

                 
                    if (typeOfSpeedLimit == TypeOfSpeedLimit.noSpeedLimit)
                    {
                        rearSpeedReducer = 1;
                    }
                    else if (typeOfSpeedLimit == TypeOfSpeedLimit.simple)
                    {
                        rearSpeedReducer = (rearMaxSpeed + currentWheelSpeed) / rearMaxSpeed;
                    }
                    else if (typeOfSpeedLimit == TypeOfSpeedLimit.squareRoot)
                    {
                        rearSpeedReducer = Mathf.Sqrt(Mathf.Abs((rearMaxSpeed + currentWheelSpeed) / rearMaxSpeed));
                    }

                  
                    wheel.wheelCollider.motorTorque = moveInput * 600 * maxAcceleration * rearSpeedReducer * Time.deltaTime;
                }
            }
        }

        void Steer() 
        {
            foreach (var wheel in wheels)
            {
                if (wheel.axel == Axel.Front)
                {
                    var _steerAngle = steerInput * turnSensitivity * maxSteerAngle;
                    wheel.wheelCollider.steerAngle = Mathf.Lerp(wheel.wheelCollider.steerAngle, _steerAngle, 0.6f);
                }
            }
        }

        void BrakeAndDeacceleration()
        {
            if (Input.GetKey(brakeKey))
            {
                foreach (var wheel in wheels)
                {
                    wheel.wheelCollider.brakeTorque = 300 * brakeAcceleration * Time.deltaTime;
                }

            }
            else if (moveInput == 0) 
            {
                foreach (var wheel in wheels)
                {
                    wheel.wheelCollider.brakeTorque = 300 * noInputDeacceleration * Time.deltaTime;
                }
            }
            else 
            {
                foreach (var wheel in wheels)
                {
                    wheel.wheelCollider.brakeTorque = 0;
                }
            }
        }

        void AnimateWheels() 
        {
            foreach (var wheel in wheels)
            {
                Quaternion rot;
                Vector3 pos;
                wheel.wheelCollider.GetWorldPose(out pos, out rot);
                wheel.wheelModel.transform.position = pos;
                wheel.wheelModel.transform.rotation = rot;
            }
        }

        void WheelEffectsCheck() 
        {
            foreach (var wheel in wheels)
            {
                
                WheelHit GroundHit; 
                wheel.wheelCollider.GetGroundHit(out GroundHit); 
                float lateralDrift = Mathf.Abs(GroundHit.sidewaysSlip);

                if (Input.GetKey(brakeKey) && wheel.axel == Axel.Rear && wheel.wheelCollider.isGrounded == true && carRb.velocity.magnitude >= brakeDriftingSkidLimit)
                {
                    EffectCreate(wheel);
                }
                else if (wheel.wheelCollider.isGrounded == true && wheel.axel == Axel.Front && (lateralDrift > lateralFrontDriftingSkidLimit)) 
                {
                    EffectCreate(wheel);
                }
                else if (wheel.wheelCollider.isGrounded == true && wheel.axel == Axel.Rear && (lateralDrift > lateralRearDriftingSkidLimit))
                {
                    EffectCreate(wheel);
                }
                else
                {
                    wheel.wheelEffectObj.GetComponentInChildren<TrailRenderer>().emitting = false;
                    carSounds.StopSkidSound(wheel.skidSound, wheel.index); 
                }
            }
        }

        private void EffectCreate(Wheel wheel) 
        {
            wheel.wheelEffectObj.GetComponentInChildren<TrailRenderer>().emitting = true;
            wheel.smokeParticle.Emit(1);
            carSounds.PlaySkidSound(wheel.skidSound);
        }

        void CarLightsControl() 
        {
            if (Input.GetKey(brakeKey)) 
            {
                carLights.RearRedLightsOn();
            }
            else
            {
                carLights.RearRedLightsOff();
            }

            if (moveInput < 0f) 
            {
                carLights.RearWhiteLightsOn();
            }
            else
            {
                carLights.RearWhiteLightsOff();
            }
        }

        void UpdateSpeedUI() 
        {
            int roundedSpeed = (int)Mathf.Round(carRb.velocity.magnitude * UISpeedMultiplier);
            speedText.text = roundedSpeed.ToString();
        }
    }
}