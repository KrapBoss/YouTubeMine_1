using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemeLab.Data
{
    [CreateAssetMenu(fileName = "AnimalStageDataSO", menuName = "MemeLab/Animal Stage Data SO")]
    public class AnimalStageDataSO : ScriptableObject
    {
        [Serializable]
        public class AnimalStageInfo
        {
            [Tooltip("단계: 1 ~ 10 (트롤링 블록은 -1)")]
            public int level = 1;
            public string animalName = "기본 동물";
            public Sprite sprite;
            public Vector2 scale = Vector2.one;
            [Tooltip("일반 동물: 1.0 ~ 3.0 (단계 비례), 트롤링 블록: 5.0")]
            public float mass = 1.0f;
            public Color debugColor = Color.white;
            public PhysicsMaterial2D physicsMaterial;
            public GameObject customPrefab;
        }

        [Header("1 ~ 10단계 동물 데이터")]
        [SerializeField] private List<AnimalStageInfo> stages = new List<AnimalStageInfo>();

        [Header("트롤링 방해 블록 데이터 (Level -1)")]
        [SerializeField] private AnimalStageInfo obstacleData = new AnimalStageInfo
        {
            level = -1,
            animalName = "무적 트롤링 블록",
            scale = new Vector2(1.5f, 1.5f),
            mass = 5.0f,
            debugColor = new Color(0.3f, 0.3f, 0.3f, 1f)
        };

        [Header("기본 물리 재질 (미지정 시 사용)")]
        [SerializeField] private PhysicsMaterial2D defaultPhysicsMaterial;

        public AnimalStageInfo GetStageInfo(int level)
        {
            if (level == -1)
            {
                return GetObstacleInfo();
            }

            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i].level == level)
                {
                    return stages[i];
                }
            }

            // Fallback 기본값 생성
            float calculatedMass = Mathf.Lerp(1.0f, 3.0f, Mathf.Clamp01((level - 1) / 9f));
            float calculatedScale = Mathf.Lerp(0.8f, 2.5f, Mathf.Clamp01((level - 1) / 9f));
            return new AnimalStageInfo
            {
                level = level,
                animalName = $"Level {level} 밈 동물",
                scale = new Vector2(calculatedScale, calculatedScale),
                mass = calculatedMass,
                debugColor = Color.HSVToRGB((level * 0.1f) % 1.0f, 0.7f, 0.9f),
                physicsMaterial = defaultPhysicsMaterial
            };
        }

        public AnimalStageInfo GetObstacleInfo()
        {
            if (obstacleData == null)
            {
                obstacleData = new AnimalStageInfo
                {
                    level = -1,
                    animalName = "무적 트롤링 블록",
                    scale = new Vector2(1.5f, 1.5f),
                    mass = 5.0f,
                    debugColor = new Color(0.2f, 0.2f, 0.2f, 1f)
                };
            }
            return obstacleData;
        }
    }
}
