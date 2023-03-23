using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amper.FPS
{
	public partial class SDKGame
	{
		public virtual bool ReadyUpEnabled() => false;
		public virtual void SimulateReadyUp()
		{
			if(sv_debug_ready)
			{
				int i = 0;
				foreach(var kv in ClientReadyStatus)
				{
					DebugOverlay.ScreenText($"Client {kv.Key.Name} is ready: {kv.Value}", new Vector2(20, 20), i, kv.Value ? Color.Green : Color.Red);
				}
			}

			if ( ClientReadyStatus.Any() && ClientReadyStatus.All( kv => kv.Value ) )
			{
				RestartGame();
			}
		}
		public virtual void StartedReadyUp() { }
		public virtual void EndedReadyUp() { }
		[Net] public IDictionary<IClient, bool> ClientReadyStatus { get; set; }

		[ConCmd.Server]
		public static void ToggleReady()
		{
			if ( Current is not SDKGame game )
				return;

			if(ConsoleSystem.Caller is IClient cl)
			{
				bool newStatus = true;
				if(game.ClientReadyStatus.ContainsKey(cl))
				{
					newStatus = !game.ClientReadyStatus[cl];
					game.ClientReadyStatus[cl] = newStatus;
				}
				else
				{
					game.ClientReadyStatus.Add(cl, newStatus );
				}

				EventDispatcher.InvokeEvent( new ClientReadyToggleEvent() { Client = cl, Status = newStatus } );
			}
		}
		[ConVar.Replicated] public static bool sv_debug_ready { get; set; } = false;
	}
}
