module CommunalHelperSolidTilesGroup

using ..Ahorn, Maple
using Ahorn.CommunalHelper

@mapdef Entity "CommunalHelper/SolidTilesGroup" SolidTilesGroup(
			x::Integer, y::Integer,
			width::Integer=16, height::Integer=16)

const placements = Ahorn.PlacementDict(
    "Solid Tiles Selection Group (Communal Helper)" => Ahorn.EntityPlacement(
        SolidTilesGroup,
		"rectangle"
    )
)

Ahorn.minimumSize(entity::SolidTilesGroup) = 16, 16
Ahorn.resizable(entity::SolidTilesGroup) = true, true

function Ahorn.selection(entity::SolidTilesGroup)
    x, y = Ahorn.position(entity)
	
    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))
	
	return Ahorn.Rectangle(x, y, width, height)
end

const block = "objects/CommunalHelper/solidExtension/tiles"

const color = (129, 62, 163, 255) ./ 255

# Gets rectangles from Solid Extensions
function getSolidRectangles(room::Maple.Room)
    entities = filter(e -> e.name == "CommunalHelper/SolidExtension", room.entities)
    rects = []

    for e in entities
        push!(rects, Ahorn.Rectangle(
            Int(get(e.data, "x", 0)),
            Int(get(e.data, "y", 0)),
            Int(get(e.data, "width", 8)),
            Int(get(e.data, "height", 8))
        ))
    end
        
    return rects
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::SolidTilesGroup, room::Maple.Room)
    rects = getSolidRectangles(room)

    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    tileWidth = ceil(Int, width / 8)
    tileHeight = ceil(Int, height / 8)

    rect = Ahorn.Rectangle(x, y, width, height)
	if !(rect in rects)
        push!(rects, rect)
    end

    for i in 1:tileWidth, j in 1:tileHeight
        drawX, drawY = (i - 1) * 8, (j - 1) * 8

        closedLeft = !notAdjacent(x, y, drawX - 8, drawY, rects)
        closedRight = !notAdjacent(x, y, drawX + 8, drawY, rects)
        closedUp = !notAdjacent(x, y, drawX, drawY - 8, rects)
        closedDown = !notAdjacent(x, y, drawX, drawY + 8, rects)
        completelyClosed = closedLeft && closedRight && closedUp && closedDown
        
        if completelyClosed
            if notAdjacent(x, y, drawX + 8, drawY + 8, rects)
                # down right
                Ahorn.drawImage(ctx, block, x + drawX + 7, y + drawY + 7, 0, 0, 8, 8, tint=color)
            end
            if notAdjacent(x, y, drawX - 8, drawY + 8, rects)
                # down left
                Ahorn.drawImage(ctx, block, x + drawX - 7, y + drawY + 7, 16, 0, 8, 8, tint=color)
            end
            if notAdjacent(x, y, drawX + 8, drawY - 8, rects)
                # up right
                Ahorn.drawImage(ctx, block, x + drawX + 7, y + drawY - 7, 0, 16, 8, 8, tint=color)
            end
            if notAdjacent(x, y, drawX - 8, drawY - 8, rects)
                # up left
                Ahorn.drawImage(ctx, block, x + drawX - 7, y + drawY - 7, 16, 16, 8, 8, tint=color)
            end

		else 
            if closedLeft && closedRight && !closedUp && closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 8, 0, 8, 8, tint=color)

            elseif closedLeft && closedRight && closedUp && !closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 8, 16, 8, 8, tint=color)

            elseif closedLeft && !closedRight && closedUp && closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 16, 8, 8, 8, tint=color)

            elseif !closedLeft && closedRight && closedUp && closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 0, 8, 8, 8, tint=color)

            elseif closedLeft && !closedRight && !closedUp && closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 16, 0, 8, 8, tint=color)

            elseif !closedLeft && closedRight && !closedUp && closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 0, 0, 8, 8, tint=color)

            elseif !closedLeft && closedRight && closedUp && !closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 0, 16, 8, 8, tint=color)

            elseif closedLeft && !closedRight && closedUp && !closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 16, 16, 8, 8, tint=color)
            end
        end

    end
end

end
