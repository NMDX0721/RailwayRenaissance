using System;
using System.IO;
using System.Text;
using System.Text.Json;
using RailwayRenaissance.Cab.Simulation;
using RailwayRenaissance.Cab.Research;

string root=args[0],id=args[1],output=args[2];using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,id+"-initial.json")));var j=doc.RootElement;
var cfg=SimulationCodec.Decode<SimulationConfig>(j.GetProperty("config").GetString());var initial=SimulationCodec.Decode<SimulationState>(j.GetProperty("state").GetString());var u=SimulationCodec.Decode<SimulationInput>(j.GetProperty("input").GetString());var task=AtoRouteTask.Decode(j.GetProperty("task").GetString());var m=new CabSimulation(initial.Seed,cfg);m.Restore(initial);
var lines=File.ReadAllLines(Path.Combine(root,id+"-commands.csv"));int cursor=1;long start=initial.Tick;var trace=new StringBuilder("elapsed,position,speed,acceleration,throttle,brake,pipe,cylinder\n");
while(cursor<lines.Length){var f=lines[cursor].Split(',');long tick=long.Parse(f[0]);while(m.State.Tick<tick){m.Step(u);}u.Throttle=int.Parse(f[3]);u.Brake=int.Parse(f[4]);if((tick-start)%25==0){var s=m.State;trace.Append(FormattableString.Invariant($"{(tick-start)*.02:R},{s.PositionM-task.Origin:R},{s.SpeedMps:R},{s.Acceleration:R},{u.Throttle},{u.Brake},{s.PipeKpa:R},{s.TrainCylinderKpa:R}\n"));}cursor++;}
File.WriteAllText(output,trace.ToString());Console.WriteLine($"REPLAY {id}: {lines.Length-1} commands, last tick {m.State.Tick}, position {m.State.PositionM-task.Origin:F3}");
