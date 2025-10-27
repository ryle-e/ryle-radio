using UnityEngine;

public class SpatialControls : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float moveSpeed;
    [SerializeField] private Vector2 limits;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
            moveSpeed *= 2;

        if (Input.GetKeyUp(KeyCode.LeftShift))
            moveSpeed /= 2;

        if (Input.GetKey(KeyCode.Q) && targetTransform.position.x > limits.x)
            targetTransform.position += moveSpeed * Time.deltaTime * Vector3.left;

        if (Input.GetKey(KeyCode.E) && targetTransform.position.x < limits.y)
            targetTransform.position += moveSpeed * Time.deltaTime * Vector3.right;
    }
}
