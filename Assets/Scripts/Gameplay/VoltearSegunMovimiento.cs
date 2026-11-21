using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class VoltearSegunMovimiento : MonoBehaviour
{
    [SerializeField] private bool spriteMiraIzquierda = false;

    private SpriteRenderer render;
    private float ultimaX;

    private void Awake()
    {
        render = GetComponent<SpriteRenderer>();
        ultimaX = transform.position.x;
    }

    public void Configurar(bool miraIzquierda)
    {
        spriteMiraIzquierda = miraIzquierda;
        ultimaX = transform.position.x;
    }

    private void LateUpdate()
    {
        float dx = transform.position.x - ultimaX;
        if (Mathf.Abs(dx) > 0.0005f)
            render.flipX = spriteMiraIzquierda ? dx > 0f : dx < 0f;
        ultimaX = transform.position.x;
    }
}