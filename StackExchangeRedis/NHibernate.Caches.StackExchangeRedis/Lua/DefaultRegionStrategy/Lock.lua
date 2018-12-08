if redis.call('set', KEYS[1], ARGV[1], 'nx', 'px', ARGV[2]) == false then
	return 0
else 
	return 1
end
