# Project N.E.W.

Protótipo técnico de um MOBA em 3D feito com **Godot 4.7.2 .NET** e **C#**.

O projeto reúne uma vertical slice jogável com duas campeãs iniciais, combate, habilidades, minions, torres, progressão e uma base de multiplayer LAN com autoridade no host.

## Recursos atuais

- Controle por clique e por teclado (`I`, `J`, `K`, `L`) para desenvolvimento.
- Câmera isométrica com acompanhamento do herói local.
- Combate básico, alvo, vida, mana, morte e reinício.
- Nyr'Vela: Energia Abissal, Orbe Abissal, Correntes do Vazio, Passo Sombrio e Domínio Abissal.
- Nyxara controlada por IA: passiva de terceiro ataque, granada, fúria, dash e Canhão Estelar.
- Ondas de minions: 3 corpo a corpo, 1 tanque e 3 ranged por equipe.
- Torres, bases, ouro, experiência, níveis e evolução de habilidades.
- Mapa central com biomas de neve, deserto, floresta e abismo.
- Multiplayer LAN host-authoritative para transformações, minions, vida, mana, progressão, ranks e VFX.

## Controles

| Ação | Tecla |
| --- | --- |
| Movimento de desenvolvimento | `I` `J` `K` `L` |
| Habilidades | `Q` `W` `E` `R` |
| Evoluir habilidade | `1` `2` `3` `4` |
| Hospedar partida LAN | `F1` |
| Entrar em localhost | `F2` |
| Reiniciar após fim de partida | `Enter` |

Também é possível clicar no terreno para mover e clicar em um alvo para atacá-lo.

## Executar

### Pré-requisitos

- Godot Engine 4.7.2 com suporte a .NET/C#.
- .NET SDK compatível com o projeto.

```powershell
dotnet build
```

Depois, abra `project.godot` pelo Godot e execute `Scenes/Game/Main.tscn`.

## Multiplayer LAN

1. No host, execute o jogo e pressione `F1`.
2. No segundo computador, execute com o argumento:

```text
-- --join=IP_DO_HOST
```

Para um teste local, pressione `F2` na segunda instância.

## Estrutura

```text
Assets/       Texturas e recursos visuais
Scenes/       Cenas Godot
Scripts/      Gameplay, IA, combate, heróis, UI e sistemas
Data/         Dados futuros de campeões e balanceamento
```

## Estado do projeto

O foco atual é polimento visual, refinamento do mapa e expansão do elenco de campeões.

