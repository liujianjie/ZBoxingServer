using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ET.Server
{
    /// <summary>
    /// ZBoxing账户数据持久化系统
    /// 使用JSON文件保存/加载玩家账户数据
    /// 存储路径: Bin/Data/zboxing_accounts.json
    /// </summary>
    [FriendOf(typeof(ZBAccountComponent))]
    public static class ZBAccountPersistSystem
    {
        private const string DataDir = "Data";
        private const string FileName = "zboxing_accounts.json";

        /// <summary>
        /// 获取存档文件完整路径
        /// </summary>
        public static string GetSaveFilePath()
        {
            // Bin/Data/zboxing_accounts.json
            string binDir = AppDomain.CurrentDomain.BaseDirectory;
            string dataDir = Path.Combine(binDir, DataDir);
            return Path.Combine(dataDir, FileName);
        }

        /// <summary>
        /// 从文件加载账户数据到组件
        /// </summary>
        public static void LoadAccounts(this ZBAccountComponent self)
        {
            self.SaveFilePath = GetSaveFilePath();

            if (!File.Exists(self.SaveFilePath))
            {
                Log.Info($"[ZBoxing] 存档文件不存在，使用空数据: {self.SaveFilePath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(self.SaveFilePath);
                var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
                ZBAccountSaveData saveData = JsonSerializer.Deserialize<ZBAccountSaveData>(json, options);

                if (saveData == null)
                {
                    Log.Warning("[ZBoxing] 存档数据为空，使用默认数据");
                    return;
                }

                self.NextPlayerId = saveData.NextPlayerId;

                foreach (ZBAccountInfo account in saveData.AccountList)
                {
                    self.Accounts[account.Username] = account;
                    self.PlayerIdToUsername[account.PlayerId] = account.Username;
                }

                Log.Info($"[ZBoxing] 加载存档成功: {saveData.AccountList.Count}个账户, NextPlayerId={self.NextPlayerId}");
            }
            catch (Exception e)
            {
                Log.Error($"[ZBoxing] 加载存档失败: {e.Message}");
            }
        }

        /// <summary>
        /// 将账户数据保存到文件
        /// </summary>
        public static void SaveAccounts(this ZBAccountComponent self)
        {
            if (!self.IsDirty)
            {
                return;
            }

            try
            {
                // 确保目录存在
                string dir = Path.GetDirectoryName(self.SaveFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // 构建存档数据
                ZBAccountSaveData saveData = new()
                {
                    NextPlayerId = self.NextPlayerId,
                    AccountList = new List<ZBAccountInfo>(self.Accounts.Values),
                };

                // 写入临时文件再重命名，避免写入中断导致数据损坏
                string tempPath = self.SaveFilePath + ".tmp";
                var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = null };
                string json = JsonSerializer.Serialize(saveData, options);
                File.WriteAllText(tempPath, json);

                // 原子替换
                if (File.Exists(self.SaveFilePath))
                {
                    File.Delete(self.SaveFilePath);
                }
                File.Move(tempPath, self.SaveFilePath);

                self.IsDirty = false;

                Log.Debug($"[ZBoxing] 存档保存成功: {saveData.AccountList.Count}个账户");
            }
            catch (Exception e)
            {
                Log.Error($"[ZBoxing] 存档保存失败: {e.Message}");
            }
        }

        /// <summary>
        /// 标记数据已变更，需要保存
        /// </summary>
        public static void MarkDirty(this ZBAccountComponent self)
        {
            self.IsDirty = true;
        }
    }
}
