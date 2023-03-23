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
			if(sv_debug_readyup)
			{
				DebugOverlay.ScreenText( "[GAME READY STATUS]", new Vector2( 20, 20 ), -1, Color.Orange, 0.1f );
				int i = 0;
				foreach(var cl in Game.Clients)
				{
					bool ready = ClientReadyStatus.ContainsKey( cl ) ? ClientReadyStatus[cl] : false;
					DebugOverlay.ScreenText($"Client {cl.Name} is ready: {ready}", new Vector2( 20, 20 ), i, ready ? Color.Green : Color.Red, 0.1f );
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

		[ConCmd.Server("sv_toggle_ready")]
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
		[ConVar.Replicated] public static bool sv_debug_readyup { get; set; } = false;
	}
}
