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
					bool ready = IsReady(cl);
					DebugOverlay.ScreenText($"Client {cl.Name} is ready: {ready}", new Vector2( 20, 20 ), i, ready ? Color.Green : Color.Red, 0.1f );
					i++;
				}
			}

			if ( ClientReadyStatus.Any() && Game.Clients.All(IsReady) )
			{
				RestartGame();
			}
		}
		public virtual void StartedReadyUp() 
		{
			ClientReadyStatus.Clear();
			/*
			foreach ( var kv in ClientReadyStatus )
				ClientReadyStatus[kv.Key] = false;
			*/
		}
		public virtual void EndedReadyUp() { }
		[Net] public IDictionary<IClient, bool> ClientReadyStatus { get; set; }
		public bool IsReady(IClient cl) => ClientReadyStatus.ContainsKey( cl ) ? ClientReadyStatus[cl] : false;

		[ConCmd.Server("toggle_ready")]
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
