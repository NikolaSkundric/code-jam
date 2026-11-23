const EMOJI = [
  '😀', '😁', '😂', '🤣', '😊', '😍', '😘', '😎',
  '🤔', '😅', '😢', '😭', '😡', '👍', '👎', '👏',
  '🙏', '🎉', '🔥', '❤️', '💔', '✅', '❌', '👀',
]

interface EmojiPickerProps {
  onPick: (emoji: string) => void
}

export default function EmojiPicker({ onPick }: EmojiPickerProps) {
  return (
    <div className="emoji-picker" role="listbox" aria-label="Emoji">
      {EMOJI.map((emoji) => (
        <button key={emoji} type="button" className="emoji-option" onClick={() => onPick(emoji)}>
          {emoji}
        </button>
      ))}
    </div>
  )
}
