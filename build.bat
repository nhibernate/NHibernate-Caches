cd SysCache
nant -t:net-3.5
cd ..

cd SysCache2
nant -t:net-3.5
cd ..

cd SharedCache
nant -t:net-3.5
cd ..

cd MemCache
nant -t:net-3.5
cd ..

cd Velocity
nant -t:net-3.5
cd ..

pause