using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.MovementTests
{
    public class PlayerMovementTests : InputTestFixture
    {
        private EditorBuildSettingsScene[] scenes;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            
            scenes = EditorBuildSettings.scenes;
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Tests/Scenes/TestScene.unity", true)
            };
            SceneManager.LoadScene("TestScene");
        }


        [UnityTest]
        public IEnumerator PlayerMovementTestsWithEnumeratorPasses()
        {
            //arrange
            yield return null;
            yield return null;
            var player = GameObject.FindGameObjectWithTag("Player");
            var startedPosition = player.transform.position;
            
            //act
            var keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.wKey, 1f);
            yield return new WaitForSeconds(1f);
            
            //assert
            Assert.That(player.transform.position.z, Is.GreaterThan(startedPosition.z));
        }

        [TearDown]
        public void Teardown()
        {
            //EditorBuildSettings.scenes = scenes;
        }
    }
}