// Decompiled with JetBrains decompiler
// Type: VehicleBehaviorEx.VehicleBehaviorExtended
// Assembly: VehicleBehavior.net, Version=0.0.0.1, Culture=neutral, PublicKeyToken=null
// MVID: 238F06A5-B873-43D7-8901-7EC83828EB6D
// Assembly location: D:\Mis Archivos\Repos\GitLab\Nation\miscres\vehiclebehavior\client\VehicleBehavior.net.dll

using CitizenFX.Core;
using CitizenFX.Core.Native;
using Microsoft.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace VehicleBehaviorEx
{
    public class VehicleBehaviorExtended : BaseScript
    {
        private Vehicle veh;

        private float CurrentSlideTorqueMult;
        private VehicleBehaviorExtended.WheelieState WState;

        public VehicleBehaviorExtended()
        {

            Tick += OnTick;
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
        }

        private void OnClientResourceStart(string resourceName)
        {
            if (API.GetCurrentResourceName() != resourceName) return;
            
        }


        private async Task OnTick()
        {
            veh = Game.Player.Character.CurrentVehicle;
            if (!Exists(veh)) return;



            if (API.GetConvarInt("it_on", 1) == 1) this.HandleInverseTorque(veh);
            if (API.GetConvarInt("cw_disable_vanilla_wheelies", 1) == 1) await this.DisableVanillaWheelie(this.veh);
            if (API.GetConvarInt("cw_enable_custom_wheelies", 1) == 1)
            {
                if ((double)API.GetVehicleHandlingFloat(((PoolObject)this.veh).Handle, "CHandlingData", "fDriveBiasFront") < 1.0)
                {
                    await this.HandleWheelies(this.veh);
                }
            }


            this.ApplyTorqueMult();
        }

        private void ApplyTorqueMult()
        {
            float mult = ((1f + this.CurrentSlideTorqueMult));
            if (mult > 1.01f) this.veh.EngineTorqueMultiplier = mult;
        }



/// <summary>
/// Calculates the intended torque multiplier.
/// </summary>
/// <param name="v"></param>
        private void HandleInverseTorque(Vehicle v)
        {

            this.CurrentSlideTorqueMult = 0.0f;

            if (v.CurrentGear <= 0 || v.HighGear <= 1) return;


            float x = (float)Math.Round(Math.Abs(VehicleBehaviorExtended.AngleBetween(Vector3.Normalize(((Entity)v).Velocity), ((Entity)v).ForwardVector)), 3);

            Vector3 velocity = v.Velocity;

            float trlat = API.GetVehicleHandlingFloat(v.Handle, "CHandlingData", "fTractionCurveLateral");
            float AWDPenalty = Map(API.GetVehicleHandlingFloat(v.Handle, "CHandlingData", "fDriveBiasFront"), 1, 0.5f, 1 - (API.GetConvarInt("it_awd_penalty_percent", 25) / 100), 1, true);


            //Handling grip * percent
            float tractionScale = API.GetVehicleMaxTraction(((PoolObject)v).Handle) * ((float)API.GetConvarInt("it_grip_scale_percent", 200) / 100);

            //1 * percent, no need to read power as the torque multiplier already accounts for it
            float powerScale = ((float)API.GetConvarInt("it_power_scale_percent", 200) / 100);

            float num2 = (float)Math.Round((double)VehicleBehaviorExtended.Map(x, trlat / 2, trlat * 2, 0.0f, tractionScale * powerScale, true), 2);
            num2 *= AWDPenalty;
            num2 *= VehicleBehaviorExtended.Map(velocity.Length(), 0.0f, 4f, 0.0f, 1f, true);


            if (Game.IsControlPressed(2, Control.Sprint)) DisplayHelpTextTimed("Torque Mult x~g~" + (1 + Math.Round(num2, 1)), 200);

            if ((double)num2 <= 0.0) return;

            this.CurrentSlideTorqueMult = num2;
        }

        private async Task DisableVanillaWheelie(Vehicle v)
        {
            if (API.GetVehicleWheelieState(((PoolObject)v).Handle) != 129) return;

            API.SetVehicleWheelieState(((PoolObject)v).Handle, 1);
        }

        private void SetWheelieState(VehicleBehaviorExtended.WheelieState state)
        {
            if (this.WState == state) return;
            this.WState = state;
        }


        /// <summary>
        /// Manages custom Wheelie states and handles the physical wheelie.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private async Task HandleWheelies(Vehicle v)
        {
            switch (this.WState)
            {
                case VehicleBehaviorExtended.WheelieState.None:
                    {

                        Vector3 velocity1 = ((Entity)v).Velocity;
                        if (velocity1.Length() >= 1.0 || (double)Game.GetControlNormal(0, Control.VehicleAccelerate) <= 0.0) break;


                        this.SetWheelieState(VehicleBehaviorExtended.WheelieState.Ready);

                        break;
                    }
                case VehicleBehaviorExtended.WheelieState.Ready:
                    {
                        if ((double)Game.GetControlNormal(0, Control.VehicleAccelerate) <= 0 || (double)Game.GetControlNormal(0, Control.VehicleMoveDown) <= 0.0)
                        {
                            this.SetWheelieState(VehicleBehaviorExtended.WheelieState.None);
                            break;
                        }

                        if ((double)Game.GetControlNormal(0, Control.VehicleMoveDown) > 0.0f && (double)Game.GetControlNormal(0, Control.VehicleAccelerate) > 0.0f && v.Speed > 0.5) this.SetWheelieState(VehicleBehaviorExtended.WheelieState.Wheelie);
                    }
                    break;
                case VehicleBehaviorExtended.WheelieState.Wheelie:
                    {
                        if ((double)v.Speed < 0.5 || (double)Game.GetControlNormal(0, Control.VehicleAccelerate) <= 0.0 || (double)Game.GetControlNormal(0, Control.VehicleMoveDown) <= 0.0)
                        {
                            this.SetWheelieState(VehicleBehaviorExtended.WheelieState.None);
                            break;
                        }


                        Vector3 spdVector = API.GetEntitySpeedVector(v.Handle, true);
                        spdVector.Normalize();

                        float spdVectorPenalty = Map(Math.Abs(spdVector.X), 0.2f, 0, 1, 0, true);
                        if (spdVectorPenalty < 0.25)
                        {
                            this.SetWheelieState(VehicleBehaviorExtended.WheelieState.None);
                            break;
                        }

                        API.GetVehicleCurrentGear(((PoolObject)v).Handle);
                        float vehicleHandlingFloat = API.GetVehicleHandlingFloat(((PoolObject)v).Handle, "CHandlingData", "fDriveBiasFront");
                        float num1 = API.GetVehicleAcceleration(((PoolObject)v).Handle) * 0.15f;

                        if ((int)v.ClassType != 4) num1 *= 0.75f;

                        float num2 = num1 * (Game.GetControlNormal(0, Control.VehicleAccelerate) * (1f - vehicleHandlingFloat)) * spdVectorPenalty;


                        if (API.HasEntityCollidedWithAnything(((PoolObject)v).Handle) || API.IsVehicleInBurnout(((PoolObject)v).Handle)) num2 *= 0.5f;

                        double num3 = (double)num2;
                        Vector3 velocity2 = ((Entity)v).Velocity;
                        double num4 = (double)VehicleBehaviorExtended.Map((velocity2).Length(), 50f, 20f, 0.75f, 1f, true);
                        float num5 = (float)(num3 * num4) * Game.GetControlNormal(0, Control.VehicleMoveDown);
                        if ((double)num5 > 0.05) num5 = 0.05f;

                        API.ApplyForceToEntity(((PoolObject)v).Handle, 3, 0.0f, 0.0f, num5, 0.0f, (float)((Entity)v).Model.GetDimensions().Y, 0.0f, 0, true, true, true, true, true);
                    }
                    break;

            }
        }

        public static bool Exists(Entity entity)
        {
            return entity != null && entity.Exists();
        }

        public static double AngleBetween(Vector3 vector1, Vector3 vector2)
        {
            return Math.Atan2((double)(vector1.X * vector2.Y - vector2.X * vector1.Y), (double)(vector1.X * vector2.X + vector1.Y * vector2.Y)) * (180.0 / Math.PI);
        }

        public static float Map(
          float x,
          float in_min,
          float in_max,
          float out_min,
          float out_max,
          bool clamp = false)
        {
            float val = (float)(((double)x - (double)in_min) * ((double)out_max - (double)out_min) / ((double)in_max - (double)in_min)) + out_min;
            if (clamp)
                val = VehicleBehaviorExtended.Clamp(val, out_min, out_max);
            return val;
        }

        public static float Clamp(float val, float min, float max)
        {
            if (val.CompareTo(min) < 0)
                return min;
            return val.CompareTo(max) > 0 ? max : val;
        }

        private void Notify(string msg, bool isImportant = false)
        {
            API.SetTextChatEnabled(false);
            API.SetNotificationTextEntry("STRING");
            API.AddTextComponentString(msg);
            API.DrawNotification(isImportant, false);
        }

        public static void DisplayHelpTextTimed(string text, int time)
        {
            API.SetTextChatEnabled(false);
            API.BeginTextCommandDisplayHelp("STRING");
            API.AddTextComponentString(text);
            API.DisplayHelpTextFromStringLabel(0, false, false, time);
        }


        public static float MStoMPH(float ms, int decimals = 3) => (float)Math.Round((double)ms * 2.23693609237671, decimals);

        public static float MPHtoMS(float mph, int decimals = 3) => (float)Math.Round((double)mph * 0.447039991617203, decimals);


        private enum WheelieState
        {
            None,
            Ready,
            Wheelie,
        }



    }

}
