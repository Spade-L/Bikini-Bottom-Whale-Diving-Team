using System;
using System.Collections.Generic;
using UnityEngine;

// 定义 GameplayInputLock 类型
public static class GameplayInputLock
{
// 更新当前逻辑
    private sealed class LockLease : IDisposable
    {
        private readonly HashSet<int> locks;
// 配置 id 数值
        private readonly int id;
        private bool released;

// 定义 LockLease 方法
        public LockLease(HashSet<int> locks, int id)
        {
// 更新当前状态
            this.locks = locks;
            this.id = id;
        }

// 定义 Dispose 方法
        public void Dispose()
        {
// 判断当前条件
            if (released)
            {
// 返回当前结果
                return;
            }

// 更新当前状态
            released = true;
            locks.Remove(id);
        }
    }

// 配置 movementLocks 数值
    private static readonly HashSet<int> movementLocks = new HashSet<int>();
    private static readonly HashSet<int> interactionLocks = new HashSet<int>();
// 配置 nextLockId 数值
    private static int nextLockId;

// 记录 IsMovementLocked 状态
    public static bool IsMovementLocked => movementLocks.Count > 0;
    public static bool IsInteractionLocked => interactionLocks.Count > 0;

// 定义 AcquireMovementLock 方法
    public static IDisposable AcquireMovementLock()
    {
// 返回当前结果
        return Acquire(movementLocks);
    }

// 定义 AcquireInteractionLock 方法
    public static IDisposable AcquireInteractionLock()
    {
// 返回当前结果
        return Acquire(interactionLocks);
    }

// 定义 Acquire 方法
    private static IDisposable Acquire(HashSet<int> locks)
    {
// 配置 id 数值
        int id = ++nextLockId;
        locks.Add(id);
// 返回当前结果
        return new LockLease(locks, id);
    }

// 运行前初始化状态
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLocks()
    {
// 调用 Clear
        movementLocks.Clear();
        interactionLocks.Clear();
// 更新当前状态
        nextLockId = 0;
    }

// 定义 ReleaseAll 方法
    public static void ReleaseAll()
    {
// 调用 Clear
        movementLocks.Clear();
        interactionLocks.Clear();
// 更新当前状态
        nextLockId = 0;
    }
}
