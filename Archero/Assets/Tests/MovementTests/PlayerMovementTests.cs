using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.MovementTests
{
    public class PlayerMovementTests  
    {
        private EditorBuildSettingsScene[] scenes;


        [SetUp]
        void Setup()
        {
            scenes = EditorBuildSettings.scenes;
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Tests/Scenes/TestScene", true)
            };
        }


        [UnityTest]
        public IEnumerator PlayerMovementTestsWithEnumeratorPasses()
        {
            SceneManager.LoadScene("TestScene");
            yield return null;
        }
        
        [TearDown]
        void Teardown()
        {
            EditorBuildSettings.scenes = scenes;
        }
    }
}