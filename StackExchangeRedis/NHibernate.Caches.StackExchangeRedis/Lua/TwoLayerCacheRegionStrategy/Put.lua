redis.call('publish', ARGV[4], ARGV[5])
if ARGV[2] == '1' then
	return redis.call('set', KEYS[1], ARGV[1], 'px', ARGV[3])
else 
	return redis.call('set', KEYS[1], ARGV[1])
end
