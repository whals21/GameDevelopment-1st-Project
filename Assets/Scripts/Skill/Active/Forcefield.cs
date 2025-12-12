using UnityEngine;
using System.Collections;

public class Forcefield : MonoBehaviour
{
    [Header("보호막 설정")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float damageInterval = 0.2f; // 초당 데미지 횟수

    private float radius;
    private float damage;
    private float damagePerTick;
    private bool isActive = false;

    // 컴포넌트
    private PolygonCollider2D polygonCollider;
    private LineRenderer lineRenderer;
    private EdgeCollider2D[] edgeColliders;

    private void Awake()
    {
        // 컴포넌트 초기화
        polygonCollider = GetComponent<PolygonCollider2D>();
        lineRenderer = GetComponent<LineRenderer>();

        // EdgeColider2D 배열 초기화
        edgeColliders = new EdgeCollider2D[8];
        for (int i = 0; i < edgeColliders.Length; i++)
        {
            edgeColliders[i] = new GameObject("Edge _{i}").AddComponent<EdgeCollider2D>();
            edgeColliders[i].transform.SetParent(transform);
            edgeColliders[i].isTrigger = true;
        }
    }

    // 보호막 초기화
    public void Init(float damage, float radius)
    {
        this.damage = damage;
        this.radius = radius;
        this.damagePerTick = damage * damageInterval; //초당 데미지를 틱당 데미지로 변환

        // 팔각형 생성
        CreateOctagonShape();

        // 시각적 효과 설정
        SetupVisualEffect();

        // 데미지 코루틴 시작
        if (!isActive)
        {
            isActive = true;
            StartCoroutine(DamageCoroutine());
        }
    }

     // 팔각형 모양 생성
     private void CreateOctagonShape()
     {
        Vector2[] octagonPoints = new Vector2[8];

        // 8개의 점으로 팔각형 생성
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.Deg2Rad / 4; // 45도 간격
            float x = radius * Mathf.Cos(angle) * radius;
            float y = radius * Mathf.Sin(angle) * radius;
            octagonPoints[i] = new Vector2(x, y);
        }

        // PolygonCollider2D 설정
        if (polygonCollider != null)
        {
            polygonCollider.points = octagonPoints;
            polygonCollider.isTrigger = true;
        }

        // LineRenderer 설정 (시각적 효과)
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = octagonPoints.Length; 
            lineRenderer.useWorldSpace = false; 
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.loop = true;

            for (int i = 0; i < octagonPoints.Length; i++)
            {
                lineRenderer.SetPosition(i, octagonPoints[i]);
            }
        }

        // EdgeCollider2D 설정
        for (int i = 0; i < edgeColliders.Length; i++)
        {
            Vector2[] edgePoints = new Vector2[2];
            edgePoints[0] = octagonPoints[i];
            edgePoints[1] = octagonPoints[(i + 1) % octagonPoints.Length]; // 다음 점과 연결

            edgeColliders[i].points = edgePoints;
            edgeColliders[i].edgeRadius = 0.1f;
        }
     }

    // 시각적 효과 설정
    private void SetupVisualEffect()
    {
        if (lineRenderer != null)
        {
            // 색상 및 머티리얼 설정
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.cyan; 
            lineRenderer.endColor = Color.cyan;
        }
    }

    // 데미지 코루틴
    private IEnumerator DamageCoroutine()
    {
        while (isActive)
        {
            // 범위 내의 모든 적에게 데미지
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);

            foreach (var enemyCol in hitEnemies)
            {
                Enemy enemy = enemyCol.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damagePerTick);
                }
            }

            // 데미지 간격만큼 대기
            yield return new WaitForSeconds(damageInterval);
        }
    }

    // 보호막 비활성화
    public void Deactivate()
    {
        isActive = false;
        StopAllCoroutines();
        Destroy(gameObject);
    }

    // 보호막 재활성화

    // 반경 업데이트

    // 데미지 업데이트

    // 디버그용 Grizmos
}