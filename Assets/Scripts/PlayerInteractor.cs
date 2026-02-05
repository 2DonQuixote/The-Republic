using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public float range = 2.0f;
    public LayerMask interactLayer; // 记得在 Unity 设置 Layer

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            PerformInteract();
        }
    }

    void PerformInteract()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, range, interactLayer);
        foreach (var col in cols)
        {
            // 尝试获取接口
            var interactable = col.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
                return; // 找到一个就停
            }
        }
    }

    // 画圈圈辅助线
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}