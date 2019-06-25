using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	private const float BASE_SPEED = 10.0f; // 移動速度の基本値
    [SerializeField] public float speed = 10.0f; // 現在の速度

    public event Action<int> OnCollision;

	private const float CHARGE_SPEED = 0.5f; // 溜める速度
	private const float MAX_ENERGY = 50.0f;  // 溜める最大値
	[SerializeField] public float energy = 0.0f; // 溜めた力

	void Start()
    {
    }

	// 不定のフレームレートで呼び出されるハンドラ
	private void Update()
	{
		// 押してる間溜める
		if (Input.GetKey(KeyCode.Space))
		{
			energy = Mathf.Min(energy + CHARGE_SPEED, MAX_ENERGY);
		}
		// 離した時に加速する
		if (Input.GetKeyUp(KeyCode.Space))
		{
			var rb = GetComponent<Rigidbody>();
			rb.velocity = new Vector3(0.0f, 0.0f, 0.0f);
			speed = BASE_SPEED * ( energy + 1.0f );
			energy = 0.0f;
		}

	}

	// 固定フレームレートで呼び出されるハンドラ
	void FixedUpdate()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        var rb = GetComponent<Rigidbody>();

        // 速度の設定
        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        rb.AddForce(movement * speed);

		// 加速状態を戻す
		if (speed > BASE_SPEED)
		{
			speed = BASE_SPEED;
		}
	}

	void OnCollisionEnter(Collision collision)
    {
        var otherPlayerController = collision.gameObject.GetComponent<OtherPlayerController>();
        if (otherPlayerController != null)
        {
            OnCollision(otherPlayerController.Id);
        }
    }
}
