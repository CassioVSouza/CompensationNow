namespace CompenseAgora.Components.Pages.Faq;

public record PerguntaFaq(string Pergunta, string RespostaHtml);

public record SecaoFaq(string Id, string Titulo, string Icone, IReadOnlyList<PerguntaFaq> Perguntas);

/// <summary>
/// Static content of the FAQ page. Answers are trusted, hand-written HTML rendered as MarkupString;
/// keep them in sync with the actual calculation rules in Features/Viagens/Calculo and Features/Energias/Calculo.
/// </summary>
public static class ConteudoFaq
{
    public static readonly IReadOnlyList<SecaoFaq> Secoes =
    [
        new("primeiros-passos", "Primeiros passos", MudBlazor.Icons.Material.Filled.RocketLaunch,
        [
            new("O que é o Compense Agora?",
                """
                <p>O Compense Agora é uma plataforma para você <strong>medir</strong> as emissões de gases de efeito estufa
                geradas pelo seu dia a dia, <strong>acompanhar</strong> a evolução delas ao longo do tempo e
                <strong>registrar</strong> as compensações que você fez.</p>
                <p>Hoje a plataforma calcula emissões de duas fontes:</p>
                <ul>
                    <li><strong>Viagens</strong> em transporte rodoviário (carro, moto, ônibus, caminhão etc.);</li>
                    <li><strong>Energia elétrica</strong> consumida da rede.</li>
                </ul>
                """),
            new("Como crio minha conta?",
                """
                <ol>
                    <li>Clique em <strong>Cadastrar</strong> no menu lateral e preencha seus dados.</li>
                    <li>Você receberá um <strong>código de confirmação</strong> no e-mail informado.</li>
                    <li>Digite o código na tela de confirmação para ativar a conta.</li>
                    <li>Pronto: use <strong>Entrar</strong> com seu e-mail e senha.</li>
                </ol>
                """),
            new("Não recebi o código de confirmação. O que faço?",
                """
                <p>Verifique primeiro as pastas de <em>spam</em> e <em>promoções</em> do seu e-mail. Se o código não
                estiver lá, use a opção <strong>Reenviar código</strong> na tela de confirmação. Confira também se o
                e-mail foi digitado corretamente no cadastro.</p>
                """),
            new("Quais são as regras para a senha?",
                """
                <p>A senha precisa ter <strong>pelo menos 8 caracteres</strong>, incluindo ao menos uma letra
                maiúscula, uma letra minúscula, um número e um caractere especial (por exemplo, <code>!</code>,
                <code>@</code> ou <code>#</code>).</p>
                """),
            new("Esqueci minha senha. Como recupero o acesso?",
                """
                <p>A recuperação de senha pela própria plataforma ainda não está disponível. Entre em contato com
                o administrador da plataforma para recuperar o acesso à sua conta.</p>
                """),
            new("Por onde começo depois de entrar?",
                """
                <p>Sugerimos este roteiro:</p>
                <ol>
                    <li>Registre suas <a href="viagens/nova">viagens</a> e seu
                    <a href="energias/nova">consumo de energia</a> dos últimos meses.</li>
                    <li>Veja no <a href="">Painel</a> quanto você emitiu e em quais meses.</li>
                    <li>Registre as <a href="compensacoes/nova">compensações</a> que você já fez e acompanhe o saldo.</li>
                </ol>
                """),
        ]),

        new("registros", "Registrando suas atividades", MudBlazor.Icons.Material.Filled.EditNote,
        [
            new("Como registro uma viagem?",
                """
                <p>Acesse <strong>Viagens → Nova viagem</strong>, informe a data de referência e escolha uma das
                três formas de registro. A emissão é calculada automaticamente assim que você salva.</p>
                """),
            new("Qual forma de registro de viagem devo escolher?",
                """
                <p>Escolha conforme a informação que você tem em mãos:</p>
                <ul>
                    <li><strong>Por combustível</strong> — você sabe <em>quantos litros</em> abasteceu e <em>qual
                    combustível</em> usou. Não é preciso escolher um veículo. É a opção mais precisa.</li>
                    <li><strong>Por tipo e ano</strong> — você sabe quantos litros consumiu e qual é o
                    <em>tipo e o ano</em> do veículo.</li>
                    <li><strong>Por distância</strong> — você só sabe <em>quantos quilômetros</em> percorreu. A
                    plataforma estima o consumo a partir do consumo médio (km/l) do tipo de veículo escolhido.</li>
                </ul>
                <p>Sempre que possível, prefira os registros baseados em litros: eles refletem o seu consumo real,
                enquanto o registro por distância usa uma média.</p>
                """),
            new("Como registro meu consumo de energia elétrica?",
                """
                <p>Acesse <strong>Energia → Novo registro de energia</strong> e informe o mês de referência e a quantidade
                consumida em <strong>kWh</strong>. Esse número aparece na sua conta de luz, normalmente no campo
                “consumo” ou “energia ativa”.</p>
                """),
            new("O que é a “data de referência”?",
                """
                <p>É a data (ou o mês) em que a atividade aconteceu — não a data em que você fez o registro. Ela
                define:</p>
                <ul>
                    <li>em qual mês a emissão aparece nos gráficos do Painel e do Relatório;</li>
                    <li>quais fatores de emissão são usados no cálculo, já que eles mudam de ano para ano (e, no
                    caso da energia, de mês para mês).</li>
                </ul>
                """),
            new("Posso editar ou excluir um registro?",
                """
                <p>Sim. Nas listas de Viagens, Energia e Compensação, use o ícone de <strong>lápis</strong> para
                editar ou o de <strong>lixeira</strong> para excluir. Ao editar, a emissão é recalculada. Antes de
                excluir, a plataforma pede confirmação. Todas essas ações ficam registradas no
                <a href="historico">Histórico</a>.</p>
                """),
            new("Não encontro meu veículo ou combustível na lista.",
                """
                <p>As listas de veículos e combustíveis seguem as categorias da metodologia de cálculo e são
                mantidas pelos administradores. Escolha a opção mais parecida com o seu caso (por exemplo, pelo
                tipo de veículo e combustível principal) ou peça ao administrador que inclua a opção que falta.</p>
                """),
        ]),

        new("quantificacao", "Como a quantificação funciona", MudBlazor.Icons.Material.Filled.Calculate,
        [
            new("O que significa “t CO2e”?",
                """
                <p><strong>Toneladas de CO2 equivalente.</strong> Cada gás de efeito estufa aquece o planeta com uma
                intensidade diferente. Para somá-los numa única medida, convertemos cada gás na quantidade de
                dióxido de carbono (CO2) que causaria o mesmo aquecimento. Essa conversão usa o
                <strong>Potencial de Aquecimento Global (GWP)</strong> de cada gás.</p>
                """),
            new("Qual metodologia a plataforma utiliza?",
                """
                <p>Os cálculos seguem o <strong>Programa Brasileiro GHG Protocol</strong>, com as fórmulas da sua
                ferramenta oficial de cálculo:</p>
                <ul>
                    <li><strong>Transporte rodoviário:</strong> fatores de emissão por combustível e por tipo e ano de
                    veículo, considerando a mistura de biocombustíveis vendida no Brasil.</li>
                    <li><strong>Energia elétrica:</strong> fator médio mensal de emissão do
                    <strong>Sistema Interligado Nacional (SIN)</strong>, publicado pelo Ministério da Ciência,
                    Tecnologia e Inovação (MCTI).</li>
                </ul>
                """),
            new("Quais gases são considerados?",
                """
                <p>Os três principais gases emitidos pela queima de combustíveis:</p>
                <ul>
                    <li><strong>Dióxido de carbono (CO2)</strong>;</li>
                    <li><strong>Metano (CH4)</strong>;</li>
                    <li><strong>Óxido nitroso (N2O)</strong>.</li>
                </ul>
                <p>Cada um é multiplicado pelo seu GWP e somado ao total em CO2e.</p>
                """),
            new("Como é calculada a emissão de uma viagem?",
                """
                <p>De forma simplificada:</p>
                <ol>
                    <li><strong>Consumo:</strong> usa os litros informados. No registro por distância, o consumo é
                    estimado: <em>distância (km) ÷ consumo médio do veículo (km/l)</em>.</li>
                    <li><strong>Separação fóssil × renovável:</strong> a gasolina vendida no Brasil contém etanol
                    anidro, e o diesel contém biodiesel. O consumo é dividido entre a parte fóssil e a parte
                    renovável de acordo com o percentual de mistura obrigatório vigente no mês da viagem.</li>
                    <li><strong>Emissão de cada gás:</strong> consumo × fator de emissão do gás para aquele
                    combustível (ou tipo e ano de veículo).</li>
                    <li><strong>Total:</strong> CO2 + CH4 × GWP do CH4 + N2O × GWP do N2O, em t CO2e.</li>
                </ol>
                """),
            new("Por que o etanol e o biodiesel “emitem menos” no cálculo?",
                """
                <p>O CO2 liberado na queima de biocombustíveis é chamado de <strong>biogênico</strong>: ele foi
                retirado da atmosfera pelas plantas (cana, soja etc.) durante o seu crescimento. Por isso, seguindo
                o GHG Protocol, esse CO2 não entra no total de emissões. Apenas o CO2 da parte
                <strong>fóssil</strong> é contado.</p>
                <p>O metano (CH4) e o óxido nitroso (N2O) liberados na queima, porém, são contabilizados tanto para a
                parte fóssil quanto para a renovável.</p>
                """),
            new("Como é calculada a emissão da energia elétrica?",
                """
                <p><strong>Emissão = consumo (kWh) × fator médio de emissão do SIN no mês de referência.</strong></p>
                <p>O fator do SIN varia mês a mês conforme a matriz elétrica brasileira. Em meses com mais uso de
                usinas termelétricas (por exemplo, em períodos de seca), cada kWh consumido gera mais emissão. Por
                isso, o mesmo consumo pode resultar em emissões diferentes em meses diferentes.</p>
                """),
            new("De onde vêm os fatores de emissão?",
                """
                <p>Das publicações oficiais usadas pelo Programa Brasileiro GHG Protocol e pelo MCTI. Os
                administradores mantêm esses fatores atualizados na plataforma. Se o fator do período da sua
                atividade ainda não foi publicado, a plataforma usa o <strong>fator mais recente disponível</strong>
                até aquela data.</p>
                """),
            new("Por que a emissão de um registro aparece como zero?",
                """
                <p>As causas mais comuns são:</p>
                <ul>
                    <li>consumo, distância ou quantidade de kWh informados como zero;</li>
                    <li>no registro por distância, o veículo escolhido ainda não tem consumo médio cadastrado;</li>
                    <li>os fatores de emissão necessários ainda não foram cadastrados pelos administradores.</li>
                </ul>
                <p>Revise o registro e, se os dados estiverem corretos, avise o administrador da plataforma.</p>
                """),
            new("Os resultados são exatos?",
                """
                <p>São <strong>estimativas</strong> baseadas em fatores médios oficiais. Elas são adequadas para
                acompanhar sua pegada e comparar períodos, mas podem diferir das emissões reais do seu veículo ou
                do seu fornecedor de energia. Quanto mais precisos os dados informados (litros em vez de
                quilômetros, por exemplo), melhor a estimativa.</p>
                """),
        ]),

        new("compensacao", "Compensação de emissões", MudBlazor.Icons.Material.Filled.Park,
        [
            new("O que é compensar emissões?",
                """
                <p>É equilibrar as emissões que você não conseguiu evitar com ações que
                <strong>retiram</strong> gases de efeito estufa da atmosfera ou <strong>evitam</strong> que sejam
                emitidos em outro lugar. Exemplos: plantio de árvores, projetos de energia renovável e compra de
                créditos de carbono certificados.</p>
                """),
            new("Compensar é o mesmo que reduzir?",
                """
                <p>Não. <strong>Reduzir vem primeiro</strong>: a emissão que não acontece é sempre a melhor opção. A
                compensação serve para neutralizar o que resta depois que você já reduziu o que era possível.</p>
                """),
            new("Como registro uma compensação?",
                """
                <p>Acesse <strong>Compensação → Nova compensação</strong> e informe:</p>
                <ul>
                    <li>a <strong>data de referência</strong>;</li>
                    <li>o <strong>tipo</strong>: Créditos de carbono, Reflorestamento, Energia renovável ou Outro;</li>
                    <li>a <strong>quantidade compensada</strong>, em t CO2e.</li>
                </ul>
                <p>A quantidade normalmente aparece no certificado ou comprovante do projeto de compensação.</p>
                """),
            new("A plataforma vende créditos de carbono ou faz a compensação por mim?",
                """
                <p>Não. O Compense Agora <strong>registra</strong> as compensações que você fez por conta própria,
                para que você acompanhe seu saldo. Guarde os comprovantes e certificados das compensações
                registradas.</p>
                """),
            new("Como sei quanto ainda preciso compensar?",
                """
                <p>No <a href="">Painel</a>, compare os cartões <strong>Emissão total</strong> e
                <strong>Compensação total</strong> do período escolhido. A diferença entre eles é o que falta
                compensar. O gráfico “Emitido vs. compensado” mostra essa evolução mês a mês.</p>
                """),
            new("Como escolher um bom projeto de compensação?",
                """
                <p>Prefira projetos com <strong>certificação reconhecida</strong> (por exemplo, Verra/VCS, Gold
                Standard ou padrões nacionais equivalentes). Verifique também se os créditos são
                <strong>aposentados</strong> (cancelados) em seu nome, para que não sejam vendidos de novo. Na
                dúvida, desconfie de promessas sem documentação.</p>
                """),
            new("Como posso reduzir minhas emissões?",
                """
                <ul>
                    <li>Prefira transporte coletivo, bicicleta, caminhada ou carona.</li>
                    <li>Em veículos flex, considere abastecer com etanol hidratado.</li>
                    <li>Mantenha o veículo revisado e os pneus calibrados.</li>
                    <li>Reduza o consumo de energia: equipamentos eficientes, iluminação LED e aparelhos desligados
                    da tomada quando fora de uso.</li>
                </ul>
                """),
        ]),

        new("acompanhamento", "Painel, relatório e histórico", MudBlazor.Icons.Material.Filled.Insights,
        [
            new("O que o Painel mostra?",
                """
                <p>O resumo das suas emissões: cartões com a emissão total, de viagens, de energia e a compensação
                total, além de gráficos mês a mês. Por padrão são exibidos os <strong>últimos 12 meses</strong>.</p>
                """),
            new("Como filtro os dados por período?",
                """
                <p>No Painel e no Relatório, use o filtro <strong>Período</strong> para escolher entre um mês
                específico, um ano, os últimos 12 meses ou um período personalizado. Depois clique em
                <strong>Aplicar filtro</strong>.</p>
                """),
            new("Como baixo o relatório?",
                """
                <p>Acesse <a href="relatorio">Relatório</a>, escolha o período e clique em
                <strong>Baixar relatório</strong>. A janela de impressão do navegador será aberta: selecione
                <strong>Salvar como PDF</strong> como destino para gerar o arquivo.</p>
                """),
            new("O que é o Histórico?",
                """
                <p>O <a href="historico">Histórico</a> lista tudo o que você fez na plataforma: registros criados,
                alterados ou excluídos (com os valores antes e depois de cada mudança) e os filtros aplicados no
                Painel e no Relatório. Ele serve para você conferir e rastrear suas ações.</p>
                """),
            new("Outras pessoas podem ver meus dados?",
                """
                <p>Não. Seus registros ficam vinculados à sua conta, e outros usuários não têm acesso a eles pela
                plataforma. Sua senha é gerenciada por um serviço de autenticação seguro e nunca fica armazenada
                no banco de dados da plataforma.</p>
                """),
        ]),
    ];
}
