using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utilities {
	public static float MoveTo(float value, float target, float speed, float deltaTime, float min = 0, float max = 1) {
		float diff = target - value;
		float delta = Mathf.Clamp(diff, -speed * deltaTime, speed * deltaTime);
		return Mathf.Clamp(value + delta, min, max);
	}

	public static Vector3 Scale6(
		Vector3 value,
		float posX, float negX,
		float posY, float negY,
		float posZ, float negZ
	) {
		Vector3 result = value;

		if (result.x > 0) {
			result.x *= posX;
		} else if (result.x < 0) {
			result.x *= negX;
		}

		if (result.y > 0) {
			result.y *= posY;
		} else if (result.y < 0) {
			result.y *= negY;
		}

		if (result.z > 0) {
			result.z *= posZ;
		} else if (result.z < 0) {
			result.z *= negZ;
		}

		return result;
	}

	public static float TransformAngle(float angle, float fov, float pixelHeight) {
		return (Mathf.Tan(angle * Mathf.Deg2Rad) / Mathf.Tan(fov / 2 * Mathf.Deg2Rad)) * pixelHeight / 2;
	}


	public static Vector3 FirstOrderIntercept(
		Vector3 shooterPosition,
		Vector3 shooterVelocity,
		float shotSpeed,
		Vector3 targetPosition,
		Vector3 targetVelocity
	) {
		Vector3 targetRelativePosition = targetPosition - shooterPosition;
		Vector3 targetRelativeVelocity = targetVelocity - shooterVelocity;
		float t = FirstOrderInterceptTime(
			shotSpeed,
			targetRelativePosition,
			targetRelativeVelocity
		);
		return targetPosition + t * (targetRelativeVelocity);
	}

	public static float FirstOrderInterceptTime(
		float shotSpeed,
		Vector3 targetRelativePosition,
		Vector3 targetRelativeVelocity
	) {
		float velocitySquared = targetRelativeVelocity.sqrMagnitude;
		if (velocitySquared < 0.001f) {
			return 0f;
		}

		float a = velocitySquared - shotSpeed * shotSpeed;

		if (Mathf.Abs(a) < 0.001f) {
			float t = -targetRelativePosition.sqrMagnitude / (2f * Vector3.Dot(targetRelativeVelocity, targetRelativePosition));
			return Mathf.Max(t, 0f);
		}

		float b = 2f * Vector3.Dot(targetRelativeVelocity, targetRelativePosition);
		float c = targetRelativePosition.sqrMagnitude;
		float determinant = b * b - 4f * a * c;

		if (determinant > 0f) {
			float t1 = (-b + Mathf.Sqrt(determinant)) / (2f * a),
					t2 = (-b - Mathf.Sqrt(determinant)) / (2f * a);
			if (t1 > 0f) {
				if (t2 > 0f)
					return Mathf.Min(t1, t2);
				else {
					return t1;
				}
			} else {
				return Mathf.Max(t2, 0f);
			}
		} else if (determinant < 0f) {
			return 0f;
		} else {
			return Mathf.Max(-b / (2f * a), 0f);
		}
	}
}
