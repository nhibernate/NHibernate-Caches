local expirationMs = ARGV[#ARGV-1]
for i=1,#KEYS-1 do
	redis.call('set', KEYS[i], ARGV[i], 'px', expirationMs)
end
