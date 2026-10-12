using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Reverb.Experiments.Mechanism.Tests
{
    public class RF01TerrainTests
    {
        [Test]
        public void GeneratedTerrain_WeightsAreNormalized_AndPathSoilStrawHaveDistinctRegions()
        {
            var terrain = AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/Experiments/RyeField/Terrain/RF01_Terrain.asset");
            Assert.That(terrain, Is.Not.Null);
            float[,,] weights = terrain.GetAlphamaps(0, 0, terrain.alphamapWidth, terrain.alphamapHeight);
            for (int z = 0; z < terrain.alphamapHeight; z++)
                for (int x = 0; x < terrain.alphamapWidth; x++)
                {
                    float sum = 0;
                    for (int layer = 0; layer < 3; layer++)
                    { Assert.That(weights[z,x,layer], Is.InRange(0f,1f)); sum += weights[z,x,layer]; }
                    Assert.That(sum, Is.EqualTo(1f).Within(.01f));
                }
            Assert.That(Weight(weights, terrain, -4f, -1f, 1), Is.GreaterThan(.9f), "Path must exist along its centreline.");
            Assert.That(Weight(weights, terrain, -2f, -4f, 0), Is.GreaterThan(.9f), "Clearing must retain dry soil.");
            Assert.That(Weight(weights, terrain, 16f, 16f, 0), Is.GreaterThan(.99f), "Rendering apron must fade back to soil without a square straw seam.");
            Assert.That(Weight(weights, terrain, 10f, 10f, 2), Is.GreaterThan(.9f), "Outer field must contain dense straw.");
        }
        static float Weight(float[,,] weights, TerrainData terrain, float x, float z, int layer)
        {
            int ix = Mathf.Clamp(Mathf.FloorToInt((x + terrain.size.x * .5f) / terrain.size.x * terrain.alphamapWidth),0,terrain.alphamapWidth-1);
            int iz = Mathf.Clamp(Mathf.FloorToInt((z + terrain.size.z * .5f) / terrain.size.z * terrain.alphamapHeight),0,terrain.alphamapHeight-1);
            return weights[iz,ix,layer];
        }
    }
}
