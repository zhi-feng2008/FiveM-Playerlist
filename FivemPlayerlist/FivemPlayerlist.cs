using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using static CitizenFX.Core.UI.Screen;

namespace FivemPlayerlist
{
    public class FivemPlayerlist : BaseScript
    {
        private int maxClients = -1;
        private bool ScaleSetup = false;
        private int currentPage = 0;
        Scaleform scale;
        private int maxPages = (int)Math.Ceiling((double)new PlayerList().Count() / 16.0);
        public struct PlayerRowConfig
        {
            public string crewName;
            public int jobPoints;
            public bool showJobPointsIcon;
        }
        private Dictionary<int, PlayerRowConfig> playerConfigs = new Dictionary<int, PlayerRowConfig>();

        private Dictionary<int, string> textureCache = new Dictionary<int, string>();

        /// <summary>
        /// Constructor
        /// </summary>
        public FivemPlayerlist()
        {
            TriggerServerEvent("fs:getMaxPlayers");
            Tick += ShowScoreboard;
            Tick += DisplayController;
            Tick += BackupTimer;

            // Periodically update the player headshots so, you don't have to wait for them later
            Tick += UpdateHeadshots;

            EventHandlers.Add("fs:setMaxPlayers", new Action<int>(SetMaxPlayers));
            EventHandlers.Add("fs:setPlayerConfig", new Action<int, string, int, bool>(SetPlayerConfig));
        }

        /// <summary>
        /// Set the config for the specified player.
        /// </summary>
        /// <param name="playerServerId"></param>
        /// <param name="crewname"></param>
        /// <param name="jobpoints"></param>
        /// <param name="showJPicon"></param>
        private async void SetPlayerConfig(int playerServerId, string crewname, int jobpoints, bool showJPicon)
        {
            var cfg = new PlayerRowConfig()
            {
                crewName = crewname ?? "",
                jobPoints = jobpoints,
                showJobPointsIcon = showJPicon
            };
            playerConfigs[playerServerId] = cfg;
            if (currentPage > -1)
                await LoadScale();
        }

        
        // 用于在常规计时器由于某种奇怪的原因未能关闭页面时关闭页面
        private async Task BackupTimer()
        {
            var timer = GetGameTimer();
            var oldPage = currentPage;
            while (GetGameTimer() - timer < 8000 && currentPage > 0 && currentPage == oldPage)
            {
                await Delay(0);
            }
            if (oldPage == currentPage)
            {
                currentPage = 0;
            }
        }

        // 根据玩家数量更新要显示的最大页面数
        private void UpdateMaxPages()
        {
            maxPages = (int)Math.Ceiling((double)new PlayerList().Count() / 16.0);
        }

        // 管理玩家列表的显示和页面设置
        private async Task DisplayController()
        {
            if (Game.IsControlJustPressed(0, Control.MultiplayerInfo))
            {
                UpdateMaxPages();
                if (ScaleSetup)
                {
                    currentPage++;
                    if (currentPage > maxPages)
                    {
                        currentPage = 0;
                    }
                    await LoadScale();
                    var timer = GetGameTimer();
                    bool nextPage = false;
                    while (GetGameTimer() - timer < 8000)
                    {
                        await Delay(1);
                        if (Game.IsControlJustPressed(0, Control.MultiplayerInfo))
                        {
                            nextPage = true;
                            break;
                        }
                    }
                    if (nextPage)
                    {
                        UpdateMaxPages();
                        if (currentPage < maxPages)
                        {
                            currentPage++;
                            await LoadScale();
                        }
                        else
                        {
                            currentPage = 0;
                        }
                    }
                    else
                    {
                        currentPage = 0;
                    }
                }
            }
        }

        // 更新最大玩家数量（由服务器事件触发）
        private void SetMaxPlayers(int count)
        {
            maxClients = count;
        }
        
        // 显示计分板
        private async Task ShowScoreboard()
        {
            if (maxClients != -1)
            {
                if (!ScaleSetup)
                {
                    await LoadScale();
                    ScaleSetup = true;
                }
                if (currentPage > 0)
                {
                    float safezone = GetSafeZoneSize();
                    float change = (safezone - 0.89f) / 0.11f;
                    float x = 50f;
                    x -= change * 78f;
                    float y = 50f;
                    y -= change * 50f;

                    var width = 400f;
                    var height = 490f;
                    if (scale != null)
                    {
                        if (scale.IsLoaded)
                        {
                            scale.Render2DScreenSpace(new System.Drawing.PointF(x, y), new System.Drawing.PointF(width, height));
                        }
                    }
                }
            }
        }
        
        // 加载界面样式
        private async Task LoadScale()
        {
            if (scale != null)
            {
                for (var i = 0; i < maxClients * 2; i++)
                {
                    scale.CallFunction("SET_DATA_SLOT_EMPTY", i);
                }
                scale.Dispose();
            }
            scale = null;
            while (!HasScaleformMovieLoaded(RequestScaleformMovie("MP_MM_CARD_FREEMODE")))
            {
                await Delay(0);
            }
            scale = new Scaleform("MP_MM_CARD_FREEMODE");
            var titleIcon = "2";
            var titleLeftText = "洛城飞行大队";
            var titleRightText = $"玩家 {NetworkGetNumConnectedPlayers()}/{maxClients}";
            scale.CallFunction("SET_TITLE", titleLeftText, titleRightText, titleIcon);
            await UpdateScale();
            scale.CallFunction("DISPLAY_VIEW");
        }

        // 用于玩家信息行选项的结构
        struct PlayerRow
        {
            public int serverId;
            public string name;
            public string rightText;
            public int color;
            public string iconOverlayText;
            public string jobPointsText;
            public string crewLabelText;
            public enum DisplayType
            {
                NUMBER_ONLY = 0,
                ICON = 1,
                NONE = 2
            };
            public DisplayType jobPointsDisplayType;
            public enum RightIconType
            {
                NONE = 0,
                INACTIVE_HEADSET = 48,
                MUTED_HEADSET = 49,
                ACTIVE_HEADSET = 47,
                RANK_FREEMODE = 65,
                KICK = 64,
                LOBBY_DRIVER = 79,
                LOBBY_CODRIVER = 80,
                SPECTATOR = 66,
                BOUNTY = 115,
                DEAD = 116,
                DPAD_GANG_CEO = 121,
                DPAD_GANG_BIKER = 122,
                DPAD_DOWN_TARGET = 123
            };
            public int rightIcon;
            public string textureString;
            public char friendType;
        }

        // 返回用于每一行中的角色头像图片的 ped 头像字符串
        private async Task<string> GetHeadshotImage(int ped)
        {
            var headshotHandle = RegisterPedheadshot(ped);
            
            // 由于某种原因，原始的循环在没有 Valid 检查或重新注册头像的情况下无法正常工作
                
            while (!IsPedheadshotReady(headshotHandle) || !IsPedheadshotValid(headshotHandle))
            {
                headshotHandle = RegisterPedheadshot(ped);
                await Delay(0);
            }
            return GetPedheadshotTxdString(headshotHandle) ?? "";
        }
        
        // 更新比例尺设置
        private async Task UpdateScale()
        {
            List<PlayerRow> rows = new List<PlayerRow>();

            for (var x = 0; x < 150; x++) // 在重新加载时进行清理，这将释放所有 ped 头像句柄 :)
            {
                UnregisterPedheadshot(x);
            }

            var amount = 0;
            foreach (Player p in new PlayerList())
            {
                if (IsRowSupposedToShow(amount))
                {
                    PlayerRow row = new PlayerRow(); // 设置为空白的 PlayerRow 对象

                    if (playerConfigs.ContainsKey(p.ServerId))
                    {
                        row = new PlayerRow()
                        {
                            color = 111,
                            crewLabelText = playerConfigs[p.ServerId].crewName,
                            friendType = ' ',
                            iconOverlayText = "",
                            jobPointsDisplayType = playerConfigs[p.ServerId].showJobPointsIcon ? PlayerRow.DisplayType.ICON :
                                (playerConfigs[p.ServerId].jobPoints >= 0 ? PlayerRow.DisplayType.NUMBER_ONLY : PlayerRow.DisplayType.NONE),
                            jobPointsText = playerConfigs[p.ServerId].jobPoints >= 0 ? playerConfigs[p.ServerId].jobPoints.ToString() : "",
                            name = p.Name.Replace("<", "").Replace(">", "").Replace("^", "").Replace("~", "").Trim(),
                            rightIcon = (int)PlayerRow.RightIconType.RANK_FREEMODE,
                            rightText = $"{p.ServerId}",
                            serverId = p.ServerId,
                        };
                    }
                    else
                    {
                        row = new PlayerRow()
                        {
                            color = 111,
                            crewLabelText = "",
                            friendType = ' ',
                            iconOverlayText = "",
                            jobPointsDisplayType = PlayerRow.DisplayType.NUMBER_ONLY,
                            jobPointsText = "",
                            name = p.Name.Replace("<", "").Replace(">", "").Replace("^", "").Replace("~", "").Trim(),
                            rightIcon = (int)PlayerRow.RightIconType.RANK_FREEMODE,
                            rightText = $"{p.ServerId}",
                            serverId = p.ServerId,
                        };
                    }

                    //Debug.WriteLine("Checking if {0} is in the Dic. Their SERVER ID {1}.", p.Name, p.ServerId);
                    if (textureCache.ContainsKey(p.ServerId))
                    {
                        row.textureString = textureCache[p.ServerId];
                    }
                    else
                    {
                        //Debug.WriteLine("Not in setting image to blank");
                        row.textureString = "";
                    }

                    rows.Add(row);
                }
                amount++;
            }
            rows.Sort((row1, row2) => row1.serverId.CompareTo(row2.serverId));
            for (var i = 0; i < maxClients * 2; i++)
            {
                scale.CallFunction("SET_DATA_SLOT_EMPTY", i);
            }
            var index = 0;
            foreach (PlayerRow row in rows)
            {
                if (row.crewLabelText != "")
                {
                    scale.CallFunction("SET_DATA_SLOT", index, row.rightText, row.name, row.color, row.rightIcon, row.iconOverlayText, row.jobPointsText,
                        $"..+{row.crewLabelText}", (int)row.jobPointsDisplayType, row.textureString, row.textureString, row.friendType);
                }
                else
                {
                    scale.CallFunction("SET_DATA_SLOT", index, row.rightText, row.name, row.color, row.rightIcon, row.iconOverlayText, row.jobPointsText,
                        "", (int)row.jobPointsDisplayType, row.textureString, row.textureString, row.friendType);
                }
                index++;
            }

            await Delay(0);
        }
        
        // 用于检查循环中的行是否应根据当前页面视图显示
        private bool IsRowSupposedToShow(int row)
        {
            if (currentPage > 0)
            {
                var max = currentPage * 16;
                var min = (currentPage * 16) - 16;
                if (row >= min && row < max)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        // 更新 "textureCache" 字典以存储在线玩家的头像
        private async Task UpdateHeadshots()
        {
            PlayerList playersToCheck = new PlayerList();

            foreach (Player p in playersToCheck)
            {
                string headshot = await GetHeadshotImage(GetPlayerPed(p.Handle));

                textureCache[p.ServerId] = headshot;
            }

            //Maybe make configurable?
            await Delay(1000);
        }

    }
}
