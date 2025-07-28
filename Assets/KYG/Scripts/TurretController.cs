using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace KYG
{
    
    public class TurretController : MonoBehaviourPun
    {
        private TurretData data;
        private TurretSlot parentSlot;

        private Transform target;
        private float attackTimer = 0f;

        public void Init(TurretData data, TurretSlot slot)
        {
            this.data = data;
            this.parentSlot = slot;
        }

        private void Update()
        {
            if(!photonView.IsMine) return;

            if (target == null || Vector3.Distance(transform.position, target.position) > data.attackRange)
            
                target = FindNearestEnemy();

            if (target != null)
            {
                attackTimer += Time.deltaTime;
                if (attackTimer >= data.attackDelay)
                {
                    FireProjectile();
                    attackTimer = 0f;
                    
                }
            }
        }

        private Transform FindNearestEnemy()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, data.attackRange, LayerMask.GetMask("Enemy"));
            if(hits.Length > 0)
                return hits[0].transform;
            return null;
        }

        private void FireProjectile()
        {
            if(target == null) return;
            
            GameObject projObj = PhotonNetwork.Instantiate(data.projectilePrefab.name, transform.position, Quaternion.identity);
            
        }
    }
}
