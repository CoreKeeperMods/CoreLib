using System.Collections;
using Unity.EditorCoroutines.Editor;

namespace CoreLib.Editor
{
    public class AsyncWrapper<T> where T : class
    {
        public EditorCoroutine coroutine { get; private set; }
        public T result;
        private IEnumerator target;
        
        public AsyncWrapper(object owner, IEnumerator target) {
            this.target = target;
            this.coroutine = EditorCoroutineUtility.StartCoroutine(Run(), owner);
        }

        private IEnumerator Run() {
            while(target.MoveNext()) {
                result = target.Current as T;
                yield return result;
            }
        }
    }
}