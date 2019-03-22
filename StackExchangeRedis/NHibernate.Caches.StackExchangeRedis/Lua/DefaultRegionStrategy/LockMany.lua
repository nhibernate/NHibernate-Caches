local lockValue = ARGV[#ARGV-2]
local expirationMs = ARGV[#ARGV-1]
local lockedKeys = {}
local lockedKeyIndex = 1
local locked = true
for i=1,#KEYS-1 do
	if redis.call('set', KEYS[i], lockValue, 'nx', 'px', expirationMs) == false then
		locked = false
		break
	else
		lockedKeys[lockedKeyIndex] = KEYS[i]
		lockedKeyIndex = lockedKeyIndex + 1
	end
end
if locked == true then
	return 1
else
	for i=1,#lockedKeys do
		redis.call('del', lockedKeys[i])
	end
	return 0
end
