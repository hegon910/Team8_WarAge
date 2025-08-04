using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace KYG
{
    /// <summary>
    /// 궁극기 발동 및 쿨타임 관리, Photon 연동
    /// </summary>
    public class UltimateSkillManager : MonoBehaviourPun
    {
        public static UltimateSkillManager Instance;

    private bool isCooldown = false;        // 쿨타임 여부
    private Coroutine cooldownRoutine;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// UI 버튼에서 호출되는 궁극기 발동 시도
    /// </summary>
    public void TryCastUltimate(UltimateSkillData skillData)
    {
        if (isCooldown || skillData == null) return;

        // 모든 클라이언트에게 발동 전달
        photonView.RPC(nameof(RPC_ExecuteSkill), RpcTarget.All, skillData.skillName);

        // 로컬 쿨타임 시작
        if (cooldownRoutine != null) StopCoroutine(cooldownRoutine);
        cooldownRoutine = StartCoroutine(StartCooldown(skillData.cooldownTime));
    }

    /// <summary>
    /// 모든 클라이언트에서 실행되는 실제 발동 로직
    /// </summary>
    [PunRPC]
    private void RPC_ExecuteSkill(string skillName)
    {
        UltimateSkillData data = SkillDatabase.Instance.GetSkillByName(skillName);
        if (data == null) return;

        ExecuteSkill(data);
    }

    /// <summary>
    /// 스킬 타입에 맞는 투사체 패턴 실행
    /// </summary>
    private void ExecuteSkill(UltimateSkillData data)
    {
        switch (data.skillType)
        {
            case UltimateSkillData.SkillType.MeteorRain:
            case UltimateSkillData.SkillType.CatapultBombard:
            case UltimateSkillData.SkillType.MissileStrike:
                StartCoroutine(SpawnProjectiles(data));
                break;
        }

        // 공통 이펙트 & 사운드 재생
        if (data.effectPrefab != null)
        {
            GameObject fx = PhotonObjectPool.Instance.Spawn(data.effectPrefab, Vector3.zero, Quaternion.identity);
            StartCoroutine(PhotonObjectPool.Instance.ReleaseAfterDelay(fx, 2f));
        }

        if (data.skillSound != null)
            AudioSource.PlayClipAtPoint(data.skillSound, Vector3.zero);
    }

    /// <summary>
    /// 여러 개의 투사체를 일정 간격으로 떨어뜨림
    /// </summary>
    private IEnumerator SpawnProjectiles(UltimateSkillData data)
    {
        for (int i = 0; i < data.projectileCount; i++)
        {
            Vector3 spawnPos = GetRandomTargetPosition();
            Vector3 startPos = spawnPos + new Vector3(0, 10f, 0); // 위에서 떨어지도록 설정

            string spawnerTag = PhotonNetwork.LocalPlayer.ActorNumber == 1 ? "P1" : "P2";
            GameObject proj = PhotonObjectPool.Instance.Spawn(data.projectilePrefab, startPos, Quaternion.identity);
            proj.GetComponent<PhotonView>().RPC("Initialize", RpcTarget.All, spawnPos, data.damage, data.areaRadius, spawnerTag);

            yield return new WaitForSeconds(0.3f);
        }
    }

    /// <summary>
    /// 랜덤한 목표 위치 반환 (화면 내 랜덤 X좌표)
    /// </summary>
    private Vector3 GetRandomTargetPosition()
    {
        float x = Random.Range(-8f, 8f);
        return new Vector3(x, 0f, 0);
    }

    /// <summary>
    /// 쿨타임 시작
    /// </summary>
    private IEnumerator StartCooldown(float time)
    {
        isCooldown = true;
        yield return new WaitForSeconds(time);
        isCooldown = false;
    }
    }
}
