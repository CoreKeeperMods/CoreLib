using System.Collections;
using Unity.EditorCoroutines.Editor;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    public class AsyncWrapper<T> where T : class
    {
        public EditorCoroutine Coroutine { get; private set; }
        public T result;
        private readonly IEnumerator _target;
        
        public AsyncWrapper(object owner, IEnumerator target) {
            _target = target;
            Coroutine = EditorCoroutineUtility.StartCoroutine(Run(), owner);
        }

        private IEnumerator Run() {
            while(_target.MoveNext()) {
                result = _target.Current as T;
                yield return result;
            }
        }
    }
}