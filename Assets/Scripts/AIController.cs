using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Windows;

public class AIController : MonoBehaviour {
    [SerializeField]
    Plane plane;
    [SerializeField]
    float steeringSpeed;
    [SerializeField]
    float minSpeed;
    [SerializeField]
    float maxSpeed;
    [SerializeField]
    float recoverSpeedMin;
    [SerializeField]
    float recoverSpeedMax;
    [SerializeField]
    LayerMask groundCollisionMask;
    [SerializeField]
    float groundCollisionDistance;
    [SerializeField]
    float groundAvoidanceAngle;
    [SerializeField]
    float groundAvoidanceMinSpeed;
    [SerializeField]
    float groundAvoidanceMaxSpeed;
    [SerializeField]
    float pitchUpThreshold;
    [SerializeField]
    float fineSteeringAngle;
    [SerializeField]
    float rollFactor;
    [SerializeField]
    float yawFactor;
    [SerializeField]
    float reactionDelayMin;
    [SerializeField]
    float reactionDelayMax;
    [SerializeField]
    float reactionDelayDistance;
    [SerializeField]
    Vector3 patrolPoint;

    Vector3 lastInput;
    bool isRecoveringSpeed;

    void Start() {
        // AI just flies in patrol patterns
        if (patrolPoint == Vector3.zero) {
            patrolPoint = plane.Rigidbody.position + plane.Rigidbody.rotation * Vector3.forward * 1000f;
        }
    }

    Vector3 AvoidGround() {
        //roll level and pull up
        var roll = plane.Rigidbody.rotation.eulerAngles.z;
        if (roll > 180f) roll -= 360f;
        return new Vector3(-1, 0, Mathf.Clamp(-roll * rollFactor, -1, 1));
    }

    Vector3 RecoverSpeed() {
        //roll and pitch level
        var roll = plane.Rigidbody.rotation.eulerAngles.z;
        var pitch = plane.Rigidbody.rotation.eulerAngles.x;
        if (roll > 180f) roll -= 360f;
        if (pitch > 180f) pitch -= 360f;
        return new Vector3(Mathf.Clamp(-pitch, -1, 1), 0, Mathf.Clamp(-roll * rollFactor, -1, 1));
    }

    Vector3 GetTargetPosition() {
        // Just fly towards patrol point
        return patrolPoint;
    }

    Vector3 CalculateSteering(float dt, Vector3 targetPosition) {
        var error = targetPosition - plane.Rigidbody.position;
        error = Quaternion.Inverse(plane.Rigidbody.rotation) * error;   //transform into local space

        var errorDir = error.normalized;
        var pitchError = new Vector3(0, error.y, error.z).normalized;
        var rollError = new Vector3(error.x, error.y, 0).normalized;
        var yawError = new Vector3(error.x, 0, error.z).normalized;

        var targetInput = new Vector3();

        var pitch = Vector3.SignedAngle(Vector3.forward, pitchError, Vector3.right);
        if (-pitch < pitchUpThreshold) pitch += 360f;
        targetInput.x = pitch;

        if (Vector3.Angle(Vector3.forward, errorDir) < fineSteeringAngle) {
            var yaw = Vector3.SignedAngle(Vector3.forward, yawError, Vector3.up);
            targetInput.y = yaw * yawFactor;
        } else {
            var roll = Vector3.SignedAngle(Vector3.up, rollError, Vector3.forward);
            targetInput.z = roll * rollFactor;
        }

        targetInput.x = Mathf.Clamp(targetInput.x, -1, 1);
        targetInput.y = Mathf.Clamp(targetInput.y, -1, 1);
        targetInput.z = Mathf.Clamp(targetInput.z, -1, 1);

        var input = Vector3.MoveTowards(lastInput, targetInput, steeringSpeed * dt);
        lastInput = input;

        return input;
    }

    float CalculateThrottle(float minSpeed, float maxSpeed) {
        float input = 0;

        if (plane.LocalVelocity.z < minSpeed) {
            input = 1;
        } else if (plane.LocalVelocity.z > maxSpeed) {
            input = -1;
        }

        return input;
    }

    void FixedUpdate() {
        if (plane.Dead) return;
        var dt = Time.fixedDeltaTime;

        Vector3 steering = Vector3.zero;
        float throttle;
        bool emergency = false;
        Vector3 targetPosition = GetTargetPosition();

        var velocityRot = Quaternion.LookRotation(plane.Rigidbody.linearVelocity.normalized);
        var ray = new Ray(plane.Rigidbody.position, velocityRot * Quaternion.Euler(groundAvoidanceAngle, 0, 0) * Vector3.forward);

        if (Physics.Raycast(ray, groundCollisionDistance + plane.LocalVelocity.z, groundCollisionMask.value)) {
            steering = AvoidGround();
            throttle = CalculateThrottle(groundAvoidanceMinSpeed, groundAvoidanceMaxSpeed);
            emergency = true;
        } else {
            if (plane.LocalVelocity.z < recoverSpeedMin || isRecoveringSpeed) {
                isRecoveringSpeed = plane.LocalVelocity.z < recoverSpeedMax;

                steering = RecoverSpeed();
                throttle = 1;
                emergency = true;
            } else {
                throttle = CalculateThrottle(minSpeed, maxSpeed);
                steering = CalculateSteering(dt, targetPosition);
            }
        }

        plane.SetThrottleInput(throttle);

        if (emergency) {
            if (isRecoveringSpeed) {
                //reduce steering strength while recovering speed
                steering.x = Mathf.Clamp(steering.x, -0.5f, 0.5f);
            }
        }

        plane.SetControlInput(steering);
    }
}
