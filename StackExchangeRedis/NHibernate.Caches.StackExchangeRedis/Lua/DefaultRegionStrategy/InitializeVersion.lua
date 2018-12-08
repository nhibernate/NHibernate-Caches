if redis.call('exists', KEYS[1]) == 1 then
	return redis.call('get', KEYS[1])
else
	redis.call('set', KEYS[1], 1)
	return 1
end
