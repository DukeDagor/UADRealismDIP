using UnityEngine;
using Il2Cpp;

#pragma warning disable CS8600
#pragma warning disable CS8603

namespace TweaksAndFixes
{
    public class CampaignMapM
    {
        public static bool CanMove(Vector3 desiredPosition, float averageRange)
        {
            if (G.ui.MovementVesselType != 1)
                return true;

            PortElement origPort = null;
            if (string.IsNullOrEmpty(G.ui.MovementFromPortId))
            {
                foreach (var tf in CampaignController.Instance.CampaignData.TaskForces)
                {
                    if (Il2CppSystem.Guid.Equals(tf.Id, G.ui.SelectedMovementGroupId))
                    {
                        origPort = tf.OriginPort;
                        break;
                    }
                }
            }
            else
            {
                CampaignMap.PortsDb.PortById.TryGetValue(G.ui.MovementFromPortId, out origPort);
            }

            if (origPort != null)
            {
                // This x calculation is a simpler version of CampaignMap.Distance,
                // which despite its name only calculates wraparound distance in x.
                float x1 = desiredPosition.x < 0f ? desiredPosition.x + CampaignMap.mapWidth : desiredPosition.x;
                float x2 = origPort.WorldCoord.x < 0f ? origPort.WorldCoord.x + CampaignMap.mapWidth : origPort.WorldCoord.x;
                float xDist = x1 - x2;
                float yDist = desiredPosition.y - origPort.WorldCoord.y;
                float zDist = desiredPosition.z - origPort.WorldCoord.z;
                float distSqr = xDist * xDist + yDist * yDist + zDist * zDist;
                var range = CampaignController.Instance.GetSubmarinesMoveDistanceLimit(true, averageRange);
                if (distSqr > range * range)
                {
                    MessageBoxUI.Show(LocalizeManager.Localize("$Ui_World_CannotMoveHere"), LocalizeManager.Localize("$Ui_World_SubCanOnlyOperateNear"));
                    return false;
                }
            }
            return true;
        }
    }
}