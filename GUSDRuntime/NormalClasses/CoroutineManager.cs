using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

namespace GUSD.Coroutine;
using Coroutine = UnityEngine.Coroutine;

public class CoroutineManager
{
    public MonoBehaviour m_MonoBehaviour;
    
    public bool isCurrentFrameEnd;
    
    private LinkedList<Coroutine> activeCoroutines = new();
    public Coroutine Start(IEnumerator ie, string methodName=null)
    {
        var coroutine = new Coroutine
        {
            m_methodName = methodName,
            m_routine = ie,
            ListNode = new LinkedListNode<Coroutine>(null)
        };
        coroutine.ListNode.Value = coroutine;
        activeCoroutines.AddLast(coroutine.ListNode);
        return coroutine;
    }
    
    public Coroutine Start(string methodName, string className, Object obj)
    {
        return null;
    }
    
    public void Stop(IEnumerator ie)
    {
        var node = activeCoroutines.First;
        while (node != null)
        {
            var next = node.Next;
            if (Equals(node.Value.m_routine, ie))
            {
                activeCoroutines.Remove(node);
                return;
            }
            node = next;
        }
    }

    public void Stop(Coroutine coroutine)
    {
        var node = activeCoroutines.First;
        while (node != null)
        {
            var next = node.Next;
            if (node.Value == coroutine)
            {
                activeCoroutines.Remove(node);
                return;
            }
            node = next;
        }
        if (coroutine.ListNode.List == activeCoroutines)
        {
            activeCoroutines.Remove(coroutine.ListNode);
        }
    }
    
    public void Stop(string methodName)
    {
        if(string.IsNullOrEmpty(methodName))
            return;
        var node = activeCoroutines.First;
        while (node != null)
        {
            var next = node.Next;
            if (Equals(methodName, node.Value.m_methodName))
            {
                activeCoroutines.Remove(node);
            }
            node = next;
        }
    }

    public bool IsActive(string methodName)
    {
        if(string.IsNullOrEmpty(methodName))
            return false;
        var node = activeCoroutines.First;
        while (node != null)
        {
            var next = node.Next;
            if (Equals(methodName, node.Value.m_methodName))
            {
                return true;
            }
            node = next;
        }
        return false;
    }

    public void StopAll()
    {
        activeCoroutines.Clear();
    }

    public void UpdateCoroutine()
    {
        var node = activeCoroutines.First;
        while (node != null)
        {
            var next = node.Next;
            ProcessCoroutine(node.Value);
            node = next;
        }
    }

    private void ProcessCoroutine(Coroutine coroutine)
    {
        object obj = coroutine.m_routine.Current;
        
        if (obj is WaitForSeconds waitForSeconds && !waitForSeconds.Tick())
        {
            return;
        }

        if (obj is WaitForSecondsRealtime waitForSecondsRealtime && !waitForSecondsRealtime.Tick())
        {
            return;
        }

        if (obj is Coroutine waitCoroutine && !waitCoroutine.IsDone)
        {
            return;
        }

        if (obj is WaitForEndOfFrame && !isCurrentFrameEnd)
        {
            return;
        }
            
        bool keepProcessing = true;
        while (keepProcessing)
        {
            IEnumerator current = coroutine.CallStack.Count > 0 ? coroutine.CallStack.Peek() : coroutine.m_routine;

            if (!current.MoveNext())
            {
                if (coroutine.CallStack.Count > 0)
                {
                    coroutine.CallStack.Pop();
                    continue;
                }
                else
                {
                    if (coroutine.ListNode != null && coroutine.ListNode.List == activeCoroutines)
                    {
                        activeCoroutines.Remove(coroutine.ListNode);
                    }
                    return;
                }
            }

            object yieldValue = current.Current;
            
            if (yieldValue is IEnumerator nested)
            {
                coroutine.CallStack.Push(current);
                coroutine.m_routine = nested;
                continue;
            }
            
            keepProcessing = false;
        }
    }
}