using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CompatibilityChecker.MonoBehaviours
{
    internal class CoroutineHandler : MonoBehaviour
    {
        private static CoroutineHandler instance;

        public static CoroutineHandler Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject coroutineHandlerObj = new GameObject("CoroutineHandler");
                    instance = coroutineHandlerObj.AddComponent<CoroutineHandler>();
                    DontDestroyOnLoad(coroutineHandlerObj);
                }
                return instance;
            }
        }

        private List<Type> runningCoroutines = new List<Type>();

        public void NewCoroutine(IEnumerator coroutine)
        {
            // Check if the same IEnumerator instance is already running
            if (!IsCoroutineRunning(coroutine.GetType()))
            {
                // Start the coroutine and store the reference
                runningCoroutines.Add(coroutine.GetType());
                StartCoroutine(ExecuteCoroutine(coroutine));
            }
            else
            {
                ModNotifyBase.logger.LogWarning($"Coroutine {coroutine.GetType().FullName} is already running");
            }
        }

        private bool IsCoroutineRunning(Type coroutine)
        {
            // Check if any IEnumerator in the list is equal to the given coroutine
            return runningCoroutines.Any(runningCoroutine => runningCoroutine == coroutine);
        }

        private IEnumerator ExecuteCoroutine(IEnumerator coroutine)
        {
            yield return StartCoroutine(coroutine);
            // Remove the coroutine from the list when it's done
            runningCoroutines.Remove(coroutine.GetType());
        }
    }
}
