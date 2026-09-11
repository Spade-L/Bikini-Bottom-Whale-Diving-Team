using System;
using System.Collections.Generic;
using UnityEngine;

// 集中管理移动和交互输入锁
public static class GameplayInputLock
{
// 推进 GameplayInputLock 的当前步骤
    private sealed class LockLease : IDisposable
    {
        private readonly HashSet<int> locks;
// 设置 LockLease 的配置数值
        private readonly int id;
        private bool released;

// 完成 LockLease 的主要职责
        public LockLease(HashSet<int> locks, int id)
        {
// 同步 LockLease 的状态
            this.locks = locks;
            this.id = id;
        }

// 处理 Dispose 对应逻辑
        public void Dispose()
        {
// 检查 Dispose 的前置条件
            if (released)
            {
// 返回 Dispose 的处理结果
                return;
            }

// 同步 Dispose 的内部状态
            released = true;
            locks.Remove(id);
        }
    }

// 设置 Dispose 的配置数值
    private static readonly HashSet<int> movementLocks = new HashSet<int>();
    private static readonly HashSet<int> interactionLocks = new HashSet<int>();
// 设置 Dispose 的配置数值（Dispose 后续步骤）
    private static int nextLockId;

// 记录 Dispose 的当前状态
    public static bool IsMovementLocked => movementLocks.Count > 0;
    public static bool IsInteractionLocked => interactionLocks.Count > 0;

// 处理 AcquireMovementLock 对应逻辑
    public static IDisposable AcquireMovementLock()
    {
// 返回 AcquireMovementLock 的处理结果
        return Acquire(movementLocks);
    }

// 处理 AcquireInteractionLock 对应逻辑
    public static IDisposable AcquireInteractionLock()
    {
// 返回 AcquireInteractionLock 的处理结果
        return Acquire(interactionLocks);
    }

// 处理 Acquire 对应逻辑
    private static IDisposable Acquire(HashSet<int> locks)
    {
// 设置 Acquire 的配置数值
        int id = ++nextLockId;
        locks.Add(id);
// 返回 Acquire 的处理结果
        return new LockLease(locks, id);
    }

// 处理 ResetLocks 对应逻辑
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLocks()
    {
// 使用 ResetLocks 所需功能
        movementLocks.Clear();
        interactionLocks.Clear();
// 同步 ResetLocks 的内部状态
        nextLockId = 0;
    }

// 处理 ReleaseAll 对应逻辑
    public static void ReleaseAll()
    {
// 使用 ReleaseAll 所需功能
        movementLocks.Clear();
        interactionLocks.Clear();
// 同步 ReleaseAll 的内部状态
        nextLockId = 0;
    }
}
