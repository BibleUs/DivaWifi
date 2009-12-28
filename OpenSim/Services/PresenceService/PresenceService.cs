/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Nini.Config;
using log4net;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Data;
using OpenSim.Services.Interfaces;
using OpenMetaverse;

namespace OpenSim.Services.PresenceService
{
    public class PresenceService : PresenceServiceBase, IPresenceService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        public PresenceService(IConfigSource config)
            : base(config)
        {
            m_log.Debug("[PRESENCE SERVICE]: Starting presence service");
        }

        public bool LoginAgent(string userID, UUID sessionID,
                UUID secureSessionID)
        {
            m_Database.Prune(userID);

            PresenceData[] d = m_Database.Get("UserID", userID);

            PresenceData data = new PresenceData();

            data.UserID = userID;
            data.RegionID = UUID.Zero;
            data.SessionID = sessionID;
            data.Data["SecureSessionID"] = secureSessionID.ToString();
            data.Data["Login"] = Util.UnixTimeSinceEpoch().ToString();
            if (d.Length > 0)
            {
                data.Data["HomeRegionID"] = d[0].Data["HomeRegionID"];
                data.Data["HomePosition"] = d[0].Data["HomePosition"];
                data.Data["HomeLookAt"] = d[0].Data["HomeLookAt"];
            }
            
            m_Database.Store(data);

            return true;
        }

        public bool LogoutAgent(UUID sessionID)
        {
            PresenceData data = m_Database.Get(sessionID);
            if (data == null)
                return false;

            PresenceData[] d = m_Database.Get("UserID", data.UserID);

            if (d.Length > 1)
            {
                m_Database.Delete("SessionID", sessionID.ToString());
                return true;
            }

            data.Data["Online"] = "false";
            data.Data["Logout"] = Util.UnixTimeSinceEpoch().ToString();

            m_Database.Store(data);

            return true;
        }

        public bool LogoutRegionAgents(UUID regionID)
        {
            m_Database.LogoutRegionAgents(regionID);

            return true;
        }


        public bool ReportAgent(UUID sessionID, UUID regionID, Vector3 position, Vector3 lookAt)
        {
            m_log.DebugFormat("[PRESENCE SERVICE]: ReportAgent with session {0} in region {1}", sessionID, regionID);
            return m_Database.ReportAgent(sessionID, regionID,
                        position.ToString(), lookAt.ToString());
        }

        public PresenceInfo GetAgent(UUID sessionID)
        {
            PresenceInfo ret = new PresenceInfo();
            
            PresenceData data = m_Database.Get(sessionID);
            if (data == null)
                return null;

            ret.UserID = data.UserID;
            ret.RegionID = data.RegionID;
            ret.Online = bool.Parse(data.Data["Online"]);
            ret.Login = Util.ToDateTime(Convert.ToInt32(data.Data["Login"]));
            ret.Logout = Util.ToDateTime(Convert.ToInt32(data.Data["Logout"]));
            ret.Position = Vector3.Parse(data.Data["Position"]);
            ret.LookAt = Vector3.Parse(data.Data["LookAt"]);
            ret.HomeRegionID = new UUID(data.Data["HomeRegionID"]);
            ret.HomePosition = Vector3.Parse(data.Data["HomePosition"]);
            ret.HomeLookAt = Vector3.Parse(data.Data["HomeLookAt"]);

            return ret;
        }

        public PresenceInfo[] GetAgents(string[] userIDs)
        {
            List<PresenceInfo> info = new List<PresenceInfo>();

            foreach (string userIDStr in userIDs)
            {
                PresenceData[] data = m_Database.Get("UserID",
                        userIDStr);

                foreach (PresenceData d in data)
                {
                    PresenceInfo ret = new PresenceInfo();

                    ret.UserID = d.UserID;
                    ret.RegionID = d.RegionID;
                    ret.Online = bool.Parse(d.Data["Online"]);
                    ret.Login = Util.ToDateTime(Convert.ToInt32(
                            d.Data["Login"]));
                    ret.Logout = Util.ToDateTime(Convert.ToInt32(
                            d.Data["Logout"]));
                    ret.Position = Vector3.Parse(d.Data["Position"]);
                    ret.LookAt = Vector3.Parse(d.Data["LookAt"]);
                    ret.HomeRegionID = new UUID(d.Data["HomeRegionID"]);
                    ret.HomePosition = Vector3.Parse(d.Data["HomePosition"]);
                    ret.HomeLookAt = Vector3.Parse(d.Data["HomeLookAt"]);

                    info.Add(ret);
                }
            }

            return info.ToArray();
        }

        public bool SetHomeLocation(string userID, UUID regionID, Vector3 position, Vector3 lookAt)
        {
            return m_Database.SetHomeLocation(userID, regionID, position, lookAt);
        }
    }
}