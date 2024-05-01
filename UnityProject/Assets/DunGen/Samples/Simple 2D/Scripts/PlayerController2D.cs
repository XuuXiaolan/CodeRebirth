using DunGen.Demo;
using UnityEngine;

namespace DunGen.Demo2D
{
	sealed class PlayerController2D : MonoBehaviour
	{
		public float MovementSpeed = 6f;

		private new CircleCollider2D collider;
		private RaycastHit2D[] hitBuffer;
		private DungeonGenerator dungeonGenerator;


		private void Start()
		{
			collider = GetComponent<CircleCollider2D>();
			hitBuffer = new RaycastHit2D[10];

			var gen = GameObject.FindObjectOfType<Generator>();
			dungeonGenerator = gen.DungeonGenerator.Generator;

			dungeonGenerator.OnGenerationStatusChanged += OnGeneratorStatusChanged;
		}

		private void OnDestroy()
		{
			dungeonGenerator.OnGenerationStatusChanged -= OnGeneratorStatusChanged;
		}

		private void OnGeneratorStatusChanged(DungeonGenerator generator, GenerationStatus status)
		{
			transform.position = Vector3.zero;
		}

		private void Update()
		{
			Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

			if(input.sqrMagnitude > 1f)
				input.Normalize();

			Vector3 direction = new Vector3(input.x, input.y, 0f);
			float distance = MovementSpeed * Time.deltaTime;
			Vector3 motion = direction * distance;

			int hitCount = collider.Cast(direction, hitBuffer, distance);

			if (hitCount > 0)
			{
				var hit = hitBuffer[0];
				motion = direction * hit.distance;
			}

			transform.position += motion;
		}
	}
}
