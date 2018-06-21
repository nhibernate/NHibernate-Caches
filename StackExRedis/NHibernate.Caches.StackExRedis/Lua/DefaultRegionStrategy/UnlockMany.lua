local lockValue = ARGV[1]
local removedKeys = 0
for i=1,#KEYS-1 do
	if redis.call('get', KEYS[i]) == lockValue then
		removedKeys = removedKeys + redis.call('del', KEYS[i])
	end
end
return removedKeys
