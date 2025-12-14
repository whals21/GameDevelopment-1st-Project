using UnityEngine;
using System.Collections;

public class Forcefield : MonoBehaviour
{
    [Header("보호막 설정")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float damageInterval = 0.2f; // 초당 데미지 횟수

    private float damage;
    private float radius;
    private float damagePerTick;
    private bool isActive = false;

    // 컴포넌트
    private PolygonCollider2D polygonCollider;
    private LineRenderer lineRenderer;
    private EdgeCollider2D[] edgeColliders; // 8개의 엣지로 팔각형 구성

    private void Awake()
    {
        // 컴포넌트 캐싱 및 초기화
        polygonCollider = GetComponent<PolygonCollider2D>();
        lineRenderer = GetComponent<LineRenderer>();

        // EdgeCollider2D 배열 초기화 및 설정
        int edgeCount = 8;
        edgeColliders = new EdgeCollider2D[edgeCount];

        for (int i = 0; i < edgeCount; i++)
        {
            GameObject edgeObj = new GameObject($"Edge_{i}");
            edgeObj.transform.SetParent(transform);

            edgeColliders[i] = edgeObj.AddComponent<EdgeCollider2D>();
            edgeColliders[i].isTrigger = true;
        }
    }

    // 보호막 초기화
    public void Init(float damage, float radius)
    {
        this.damage = damage;
        this.radius = radius;
        this.damagePerTick = damage * damageInterval; // 초당 데미지를 틱당 데미지로 변환

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
        // 팔각형 설정 상수화
        const int OCTAGON_SIDES = 8;
        const float ANGLE_STEP = 45f;

        // 로컬 좌표로 팔각형 점 계산 (한 번만 계산하면 됨)
        Vector3[] octagonPoints = new Vector3[OCTAGON_SIDES];
        Vector2[] colliderPoints = new Vector2[OCTAGON_SIDES];

        for (int i = 0; i < OCTAGON_SIDES; i++)
        {
            float angleRad = i * ANGLE_STEP * Mathf.Deg2Rad;
            float x = Mathf.Cos(angleRad) * radius;
            float y = Mathf.Sin(angleRad) * radius;

            octagonPoints[i] = new Vector3(x, y, 0);
            colliderPoints[i] = new Vector2(x, y);
        }

        // LineRenderer 설정 (로컬 좌표 사용 - 자동으로 객체 따라다님)
        SetupLineRenderer(octagonPoints);

        // PolygonCollider2D 설정 (로컬 좌표)
        SetupPolygonCollider(colliderPoints);

        // EdgeCollider2D 설정
        SetupEdgeColliders(colliderPoints);
    }

    // LineRenderer 설정 분리
    private void SetupLineRenderer(Vector3[] octagonPoints)
    {
        if (lineRenderer == null) return;

        lineRenderer.useWorldSpace = false; // 로컬 좌표 사용으로 변경!
        lineRenderer.positionCount = octagonPoints.Length; // 8개 점만 필요 (loop이 자동 연결)
        lineRenderer.startWidth = 0.3f; // 더 잘 보이도록 너비 증가
        lineRenderer.endWidth = 0.3f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(0f, 0.5f, 1f, 0.8f); // 더 선명한 파란색
        lineRenderer.endColor = new Color(0f, 0.5f, 1f, 0.8f);
        lineRenderer.loop = true; // 자동으로 시작점으로 연결
        lineRenderer.sortingOrder = 10; // 렌더링 순서 설정

        // 로컬 좌표로 점 설정 (객체가 이동하면 자동으로 월드 좌표로 변환됨)
        for (int i = 0; i < octagonPoints.Length; i++)
        {
            lineRenderer.SetPosition(i, octagonPoints[i]);
        }
    }

    // PolygonCollider2D 설정 분리
    private void SetupPolygonCollider(Vector2[] colliderPoints)
    {
        if (polygonCollider == null) return;

        polygonCollider.points = colliderPoints;
        polygonCollider.isTrigger = true;
    }

    // EdgeCollider2D 설정 분리
    private void SetupEdgeColliders(Vector2[] colliderPoints)
    {
        const float EDGE_RADIUS = 0.1f;

        for (int i = 0; i < edgeColliders.Length; i++)
        {
            if (edgeColliders[i] == null) continue;

            Vector2[] edgePoints = new Vector2[2];
            edgePoints[0] = colliderPoints[i];
            edgePoints[1] = colliderPoints[(i + 1) % edgeColliders.Length]; // 다음 점과 연결

            edgeColliders[i].points = edgePoints;
            edgeColliders[i].edgeRadius = EDGE_RADIUS;

            // 부메랑 통과를 위해 일시적으로 비활성화
            edgeColliders[i].enabled = false;
        }
    }

    // 시각적 효과 설정
    private void SetupVisualEffect()
    {
        // CreateOctagonShape에서 모든 설정을 처리하므로 이 메서드는 비워둠
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

    // 비활성화
    public void Deactivate()
    {
        isActive = false;

        // 시각적 효과 제거
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        // 콜라이더 비활성화
        if (polygonCollider != null)
        {
            polygonCollider.enabled = false;
        }

        foreach (var edge in edgeColliders)
        {
            if (edge != null)
            {
                edge.enabled = false;
            }
        }
    }

    // 재활성화
    public void Reactivate()
    {
        if (!isActive)
        {
            Init(damage, radius);
        }
    }

    // 반경 업데이트
    public void UpdateRadius(float newRadius)
    {
        radius = newRadius;
        CreateOctagonShape();
    }

    // 위치 업데이트 (로컬 좨표이므로 Transform만 변경하면 됨)
    public void UpdatePosition(Vector3 newPosition)
    {
        transform.position = newPosition;
        // CreateOctagonShape() 호출할 필요 없음 - 로컬 좌표이므로 자동으로 따라다님
    }

    // 데미지 업데이트
    public void UpdateDamage(float newDamage)
    {
        damage = newDamage;
        damagePerTick = damage * damageInterval;
    }

    // 디버그용 Gizmo
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radius);

        // 팔각형 그리기
        Gizmos.color = Color.cyan;
        for (int i = 0; i < 8; i++)
        {
            float angle1 = i * 45f * Mathf.Deg2Rad;
            float angle2 = ((i + 1) % 8) * 45f * Mathf.Deg2Rad;

            Vector3 p1 = transform.position + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0);
            Vector3 p2 = transform.position + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0);

            Gizmos.DrawLine(p1, p2);
        }
    }
}