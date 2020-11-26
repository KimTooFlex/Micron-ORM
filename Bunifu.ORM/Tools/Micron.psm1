$answers =  "As I see it, yes", 
            "Reply hazy, try again", 
            "Outlook not so good"

function Micron-Update($question) {
    $answers | Get-Random
}

Register-TabExpansion 'Micron-Update' @{
    'question' = { 
        "Is this my lucky day?",
        "Will it rain tonight?",
        "Do I watch too much TV?"
    }
}

Export-ModuleMember Micron-Update