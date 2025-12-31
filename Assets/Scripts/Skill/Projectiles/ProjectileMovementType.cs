using UnityEngine;

/// <summary>
/// 투사체 이동 방식을 정의하는 열거형
/// </summary>
public enum ProjectileMovementType
{
    Straight,   // 직선 이동 (v1 Projectile)
    Boomerang,  // 부메랑 (v1 BoomerangProjectile) - 전진 후 회귀
    Homing,     // 유도 (v1 Missile/RPG) - 적 추적
    Orbit,      // 공전 (v1 MagneticDart) - 플레이어 주변 공전
    Molotov,    // 몰로토프 (v1 MolotovProjectile) - 포물선 비행 후 지면 폭발
    Brick,      // 벽돌 (v1 BrickProjectile) - 중력 기반 포물선 + 바운스
    Soccer      // 축구공 (v1 SoccerBallProjectile) - 화면 밖 재발사 + 튕김
}
