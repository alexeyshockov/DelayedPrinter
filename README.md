# DelayedPrinter

Run as usual with `dotnet run`, test as usual with `dotnet test`.

The delay algorithm is implemented using Redis' `ZSET` (sorted set) and a `PUBSUB` queue. When a print request arrives, 
it's put to the scheduler's `ZSET` using requested print time (timestamp, resolution up to a second). The scheduler 
checks this `ZSET` periodically, with a small random delay (to lower race conditions if there are multiple schedulers 
running). When a message is ready to be printed, the scheduler immediately moves it to the print queue (`PUBSUB`) (the 
move operation is executed under a lock, to eliminate race conditions between schedulers; the operation also wrapped in 
a transaction, to not loose anything).  
