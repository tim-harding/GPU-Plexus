namespace Grid
{
	using UnityEngine;

	[RequireComponent(typeof(Camera))]
	public class ViewportCamera : MonoBehaviour
	{
		public float MoveSpeed = 1.0f;
		public float RotateSpeed = 1.0f;

		private Vector3 Rotation = Vector3.zero;

		private void Start()
		{
			Rotation = transform.localEulerAngles;
			Rotation.z = 0f;
		}

		private void Update()
		{
			var directions = new Vector3(
				Input.GetAxis("Horizontal"),
				Input.GetAxis("Dolly"),
				Input.GetAxis("Vertical")
			);

			transform.Translate(directions * MoveSpeed);

			if (Input.GetMouseButton(1))
			{
				Rotation += new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0f) * RotateSpeed;
				transform.rotation = Quaternion.Euler(Rotation);
			}
		}
	}
}