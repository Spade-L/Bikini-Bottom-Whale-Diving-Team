using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 为场景演出提供可恢复的移动与普通交互锁。每次获取都会返回独立令牌，只有该令牌能释放对应锁。
/// </summary>
public static class GameplayInputLock
{
    private sealed class LockLease : IDisposable
    {
        private readonly HashSet<int> locks;
        private readonly int id;
        private bool released;

        public LockLease(HashSet<int> locks, int id)
        {
            this.locks = locks;
            this.id = id;
        }

        public void Dispose()
        {
            if (released)
            {
                return;
            }

            released = true;
            locks.Remove(id);
        }
    }

    private static readonly HashSet<int> movementLocks = new HashSet<int>();
    private static readonly HashSet<int> interactionLocks = new HashSet<int>();
    private static int nextLockId;

    public static bool IsMovementLocked => movementLocks.Count > 0;
    public static bool IsInteractionLocked => interactionLocks.Count > 0;

    public static IDisposable AcquireMovementLock()
    {
        return Acquire(movementLocks);
    }

    public static IDisposable AcquireInteractionLock()
    {
        return Acquire(interactionLocks);
    }

    private static IDisposable Acquire(HashSet<int> locks)
    {
        int id = ++nextLockId;
        locks.Add(id);
        return new LockLease(locks, id);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLocks()
    {
        movementLocks.Clear();
        interactionLocks.Clear();
        nextLockId = 0;
    }
}
