local expirationMs = ARGV[#ARGV]
for i=1,#KEYS do
	redis.call('set', KEYS[i], ARGV[i], 'px', expirationMs)
end
