module CommunalHelperDreamSwapBlock

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/DreamSwapBlock" DreamSwapBlock(x::Integer, y::Integer, 
	width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight, 
	noReturn::Bool=false, featherMode::Bool=false, oneUse::Bool=false) 

const placements = Ahorn.PlacementDict(
    "Dream Swap Block (Communal Helper)" => Ahorn.EntityPlacement(
        DreamSwapBlock,
        "rectangle",
        Dict{String, Any}(),
        Ahorn.SwapBlock.swapFinalizer
    )
)

Ahorn.nodeLimits(entity::DreamSwapBlock) = 1, 1

Ahorn.minimumSize(entity::DreamSwapBlock) = 16, 16
Ahorn.resizable(entity::DreamSwapBlock) = true, true

const crossSprite = "objects/CommunalHelper/dreamMoveBlock/x"

function Ahorn.selection(entity::DreamSwapBlock)
    x, y = Ahorn.position(entity)
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [Ahorn.Rectangle(x, y, width, height), Ahorn.Rectangle(stopX, stopY, width, height)]
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::DreamSwapBlock)
    sprite = get(entity.data, "sprite", "block")
    startX, startY = Int(entity.data["x"]), Int(entity.data["y"])
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.CommunalHelper.renderCustomDreamBlock(ctx, stopX, stopY, width, height, get(entity.data, "featherMode", false), get(entity.data, "oneUse", false))
    Ahorn.drawArrow(ctx, startX + width / 2, startY + height / 2, stopX + width / 2, stopY + height / 2, Ahorn.colors.selection_selected_fc, headLength=6)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::DreamSwapBlock)
    sprite = get(entity.data, "sprite", "block")

    startX, startY = Int(entity.data["x"]), Int(entity.data["y"])
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.SwapBlock.renderTrail(ctx, min(startX, stopX), min(startY, stopY), abs(startX - stopX) + width, abs(startY - stopY) + height, "objects/swapblock/target")
    Ahorn.CommunalHelper.renderCustomDreamBlock(ctx, startX, startY, width, height, get(entity.data, "featherMode", false), get(entity.data, "oneUse", false))

    if Bool(get(entity.data, "noReturn", false))
        noReturnSprite = Ahorn.getSprite(crossSprite, "Gameplay")
        Ahorn.drawImage(ctx, noReturnSprite, startX + div(width - noReturnSprite.width, 2), startY + div(height - noReturnSprite.height, 2))
    end
end

end