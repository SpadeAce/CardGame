using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Actor : TileEntity
{
    private const float MoveSpeed = 3f;

    public Animator animator;    
    [SerializeField] 
    private Spawn _spawnHud;
    

    private bool _isMoving;
    private HUD_Actor _hudActor;

    public void Init(DPawn pawn)
    {
        SetData(pawn);
        _hudActor = _spawnHud.Get<HUD_Actor>();
        if (_hudActor != null)
            _hudActor.Init(pawn);
        pawn.onFloatingText += OnFloatingText;
    }

    public void Init(DMonster monster)
    {
        SetData(monster);
        _hudActor = _spawnHud.Get<HUD_Actor>();
        if (_hudActor != null)
            _hudActor.Init(monster);
        monster.onFloatingText += OnFloatingText;
    }

    private void OnFloatingText(FloatingTextType type, int value)
    {
        string text = type switch
        {
            FloatingTextType.Damage => $"-{value}",
            FloatingTextType.Heal   => $"+{value}",
            FloatingTextType.Shield => $"+{value}",
            FloatingTextType.Block  => "BLOCK",
            FloatingTextType.Miss   => "MISS",
            FloatingTextType.Buff   => $"▲{value}",
            FloatingTextType.Debuff => $"▼{value}",
            _ => value.ToString()
        };
        UIManager.Instance.Hud?.ShowFloatingText(type, text, transform.position, transform);
    }

    private void OnDestroy()
    {
        if (Data is DPawn pawn)
            pawn.onFloatingText -= OnFloatingText;
        else if (Data is DMonster monster)
            monster.onFloatingText -= OnFloatingText;
    }

    public void SetSpeed(float speed)
    {
        animator.SetFloat("speed", speed);
    }

    /// <summary>
    /// 대상을 바라보고 "Attack01" 애니메이터 스테이트를 재생한다.
    /// 애니메이션이 끝나면 onComplete를 호출한다.
    /// </summary>
    public void PerformAttack(Actor target, System.Action onComplete = null)
    {
        StartCoroutine(AttackCoroutine(target, onComplete));
    }

    private IEnumerator AttackCoroutine(Actor target, System.Action onComplete)
    {
        Vector3 dir = target.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);

        animator.Play("Attack01");

        // 한 프레임 대기 후 애니메이터가 상태를 업데이트할 때까지 기다린다
        yield return null;
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack01"))
            yield return null;

        // Attack01 상태가 끝날 때까지 대기
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;

        onComplete?.Invoke();
    }

    /// <summary>
    /// 공격자를 바라보고 "GetHit" 애니메이터 스테이트를 재생한다.
    /// 애니메이션이 끝나면 onComplete를 호출한다.
    /// </summary>
    public void ReceiveHit(Actor attacker, System.Action onComplete = null)
    {
        StartCoroutine(HitCoroutine(attacker, onComplete));
    }

    private IEnumerator HitCoroutine(Actor attacker, System.Action onComplete)
    {
        Vector3 dir = attacker.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);

        animator.Play("GetHit");

        yield return null;
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("GetHit"))
            yield return null;
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;

        onComplete?.Invoke();
    }

    /// <summary>
    /// 공격자를 바라보고 "Evade" 애니메이터 스테이트를 재생한다.
    /// 애니메이션이 끝나면 onComplete를 호출한다.
    /// </summary>
    public void Evade(Actor attacker, System.Action onComplete = null)
    {
        StartCoroutine(EvadeCoroutine(attacker, onComplete));
    }

    private IEnumerator EvadeCoroutine(Actor attacker, System.Action onComplete)
    {
        Vector3 dir = attacker.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);

        animator.Play("Evade");

        yield return null;
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Evade"))
            yield return null;
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;

        onComplete?.Invoke();
    }

    /// <summary>
    /// "Die" 애니메이터 스테이트를 재생한 뒤 타일 슬롯에서 분리하고 오브젝트를 숨긴다.
    /// </summary>
    public void Die(System.Action onComplete = null)
    {
        StartCoroutine(DieCoroutine(onComplete));
    }

    private IEnumerator DieCoroutine(System.Action onComplete)
    {
        animator.Play("Die");

        yield return null;
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Die"))
            yield return null;
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;

        if (Slot != null) Slot.ClearEntity();
        gameObject.SetActive(false);

        onComplete?.Invoke();
    }

    public void MoveTo(IReadOnlyList<SquareTile> path, System.Action onComplete = null)
    {
        if (_isMoving || path == null || path.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(MoveCoroutine(path, onComplete));
    }

    private IEnumerator MoveCoroutine(IReadOnlyList<SquareTile> path, System.Action onComplete)
    {
        _isMoving = true;

        // 출발 슬롯에서 분리
        Slot.ClearEntity();
        transform.SetParent(null);

        // 웨이포인트 순서대로 이동
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 targetPos = path[i].Slot.transform.position;

            // 다음 타일 방향으로 회전
            Vector3 dir = targetPos - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0f)
                transform.rotation = Quaternion.LookRotation(dir);

            SetSpeed(1f);
            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, targetPos, MoveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos;
        }

        // 도착: 마지막 타일 슬롯에 부착
        SetSpeed(0f);
        Quaternion arrivedRotation = transform.rotation;
        path[path.Count - 1].Slot.SetEntity(this);
        transform.rotation = arrivedRotation;

        _isMoving = false;
        onComplete?.Invoke();
    }
}
