cd SysCache
nant -t:net-3.5 test
cd ..

cd SysCache2
nant -t:net-3.5
cd ..

pause