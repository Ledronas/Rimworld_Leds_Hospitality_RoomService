Update 7/5/26
fixed yellow error
Type BuildingBedSolicitationGizmoPatch probably needs a StaticConstructorOnStartup attribute, because it has a field ToggleIcon of type Texture2D. All assets must be loaded in the main thread.
UnityEngine.StackTraceUtility:ExtractStackTrace ()
Verse.Log:Warning (string)
Verse.StaticConstructorOnStartupUtility/<>c__DisplayClass2_0:<ReportProbablyMissingAttributes>b__0 (System.Type)
System.Threading.Tasks.Parallel/<>c__DisplayClass33_0`2<System.Type, object>:<ForEachWorker>b__0 (int)
System.Threading.Tasks.Parallel/<>c__DisplayClass19_0`1<object>:<ForWorker>b__1 (System.Threading.Tasks.RangeWorker&,int,bool&)
System.Threading.Tasks.TaskReplicator/Replica`1<System.Threading.Tasks.RangeWorker>:ExecuteAction (bool&)
System.Threading.Tasks.TaskReplicator/Replica:Execute ()
System.Threading.Tasks.TaskReplicator/Replica/<>c:<.ctor>b__4_0 (object)
System.Threading.Tasks.Task:InnerInvoke ()
System.Threading.Tasks.Task:Execute ()
System.Threading.Tasks.Task:ExecutionContextCallback (object)
System.Threading.ExecutionContext:RunInternal (System.Threading.ExecutionContext,System.Threading.ContextCallback,object,bool)
System.Threading.ExecutionContext:Run (System.Threading.ExecutionContext,System.Threading.ContextCallback,object,bool)
System.Threading.Tasks.Task:ExecuteWithThreadLocal (System.Threading.Tasks.Task&)
System.Threading.Tasks.Task:ExecuteEntry (bool)
System.Threading.Tasks.Task:System.Threading.IThreadPoolWorkItem.ExecuteWorkItem ()
System.Threading.ThreadPoolWorkQueue:Dispatch ()
System.Threading._ThreadPoolWaitCallback:PerformWaitCallback ()


Update 7/5/26
Fixed the thoughts hediff thanks to zaljerem on steam

update 7/4/26
Added right click to test solictiting and to show why they arent doing it. 

