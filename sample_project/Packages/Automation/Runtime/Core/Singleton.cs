
using UnityEngine;


namespace Automation.Runtime.Core
{

    public class SingletonMonoBehaviour<T> :
        MonoBehaviour
        where T : SingletonMonoBehaviour<T>
    {

        //---------------------------------------------------------------------
        private static volatile T sInstance;
        private bool mDestroying;

        //---------------------------------------------------------------------
        public static T Instance
        {
            get
            {
                return sInstance;
            }
        }

        //---------------------------------------------------------------------
        private void Awake()
        {
            if (sInstance == null)
            {
                sInstance = (T)this;

                OnAwake();

                if (gameObject.transform.parent == null)
                    DontDestroyOnLoad(gameObject);
            }
            else
            {
                Shutdown();
            }
        }

        //---------------------------------------------------------------------
        protected virtual void OnAwake()
        { }

        //---------------------------------------------------------------------
        protected virtual void OnDestroy()
        {
            mDestroying = true;
            if (sInstance != null)
            {
                sInstance = null;
            }
        }

        //---------------------------------------------------------------------
        public virtual void Shutdown()
        {
            if (sInstance != null && !mDestroying)
            {
                Destroy(gameObject);
            }
        }
    }
}